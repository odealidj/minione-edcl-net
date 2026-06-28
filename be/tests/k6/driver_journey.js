import http from 'k6/http';
import { check, sleep, fail } from 'k6';
import { SharedArray } from 'k6/data';

// Configuration
export const options = {
    scenarios: {
        driver_journey: {
            executor: 'shared-iterations',
            vus: 50,
            iterations: 100, // Make sure this matches the number of seeded users (targetVUs)
            maxDuration: '2m',
        },
    },
    thresholds: {
        'http_req_duration': ['p(95)<500'], // 95% of requests should be below 500ms
        'http_req_failed': ['rate<0.01'],   // Error rate should be less than 1%
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5140/api/v1';

// Load seeded users created by the C# Seeder
const users = new SharedArray('users', function () {
    return JSON.parse(open('./users.json'));
});

// UUID generator for Idempotency-Key
function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        let r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

import exec from 'k6/execution';
import { Trend } from 'k6/metrics';

const loginTrend = new Trend('req_login_duration');
const dashboardTrend = new Trend('req_dashboard_duration');
const startJobTrend = new Trend('req_startJob_duration');
const scanKanbanTrend = new Trend('req_scanKanban_duration');
const completeStopTrend = new Trend('req_completeStop_duration');
const endJobTrend = new Trend('req_endJob_duration');

export default function () {
    // Each iteration picks a unique user from 0 to 99
    const userIndex = exec.scenario.iterationInTest % users.length;
    const user = users[userIndex];

    if (!user) {
        fail('No user data found. Did you run the C# seeder?');
    }

    // -------------------------------------------------------------------------
    // 1. DRIVER LOGIN
    // -------------------------------------------------------------------------
    let res = http.post(`${BASE_URL}/auth/drivers/login`, JSON.stringify({
        phoneNumber: user.phone,
        pin: user.pin
    }), {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'Login' }
    });
    loginTrend.add(res.timings.duration);

    check(res, {
        'Login successful': (r) => r.status === 200,
    });

    if (res.status !== 200) return; // Stop if login fails

    const token = res.json('data.accessToken');
    const authHeaders = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
    };

    sleep(1); // User looks at dashboard

    // -------------------------------------------------------------------------
    // 2. GET DASHBOARD
    // -------------------------------------------------------------------------
    res = http.get(`${BASE_URL}/jobs/dashboard`, { headers: authHeaders, tags: { name: 'Dashboard' } });
    dashboardTrend.add(res.timings.duration);
    check(res, {
        'Dashboard loaded': (r) => r.status === 200,
    });

    sleep(1); // User taps "Start Job"

    // -------------------------------------------------------------------------
    // 3. START JOB
    // -------------------------------------------------------------------------
    authHeaders['X-Idempotency-Key'] = uuidv4();
    res = http.post(`${BASE_URL}/jobs/${user.pickupOrderId}/start`, JSON.stringify({
        pickupOrderId: user.pickupOrderId,
        truckId: null
    }), { headers: authHeaders, tags: { name: 'StartJob' } });
    startJobTrend.add(res.timings.duration);
    
    check(res, {
        'Job started': (r) => r.status === 200,
    });
    delete authHeaders['X-Idempotency-Key']; // cleanup

    sleep(2); // User arrives at supplier and scans kanban

    // -------------------------------------------------------------------------
    // 4. SCAN KANBAN
    // -------------------------------------------------------------------------
    authHeaders['X-Idempotency-Key'] = uuidv4();
    res = http.post(`${BASE_URL}/jobs/stops/${user.stopId}/manifests/${user.manifestId}/kanban`, JSON.stringify({
        kanbanCode: user.kanbanCode
    }), { headers: authHeaders, tags: { name: 'ScanKanban' } });
    scanKanbanTrend.add(res.timings.duration);

    check(res, {
        'Kanban scanned': (r) => r.status === 200,
    });
    delete authHeaders['X-Idempotency-Key'];

    sleep(1); // User taps "Complete Stop"

    // -------------------------------------------------------------------------
    // 5. COMPLETE STOP
    // -------------------------------------------------------------------------
    authHeaders['X-Idempotency-Key'] = uuidv4();
    res = http.post(`${BASE_URL}/jobs/stops/${user.stopId}/complete`, JSON.stringify({
        latitude: user.supplierLat || -6.2,
        longitude: user.supplierLng || 106.8
    }), { headers: authHeaders, tags: { name: 'CompleteStop' } });
    completeStopTrend.add(res.timings.duration);

    check(res, {
        'Stop completed': (r) => r.status === 200,
    });
    delete authHeaders['X-Idempotency-Key'];

    sleep(1); // User taps "End Job"

    // -------------------------------------------------------------------------
    // 6. END JOB
    // -------------------------------------------------------------------------
    authHeaders['X-Idempotency-Key'] = uuidv4();
    res = http.post(`${BASE_URL}/jobs/${user.pickupOrderId}/end`, JSON.stringify({}), { headers: authHeaders, tags: { name: 'EndJob' } });
    endJobTrend.add(res.timings.duration);

    check(res, {
        'Job ended': (r) => r.status === 200,
    });
    delete authHeaders['X-Idempotency-Key'];

    // End of scenario. VU will loop back if duration allows, but our jobs are marked as ended.
    // If it loops, it will fail on next iteration because the job is completed.
    // In ramping VUs, if VUs iterate, we need enough jobs. The seeder generates N jobs for N VUs.
    // The script runs for 2 mins, a full journey takes ~6 seconds + request times. 
    // If you need sustained load, the seeder should generate way more jobs, or the API should reset jobs on completion.
    // We handle the loop issue in this demo by just ignoring subsequent failures or just testing single iterations.
}
