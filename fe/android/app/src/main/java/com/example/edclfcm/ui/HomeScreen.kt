package com.example.edclfcm.ui

import android.Manifest
import android.os.Build
import android.util.Log
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.example.edclfcm.api.ApiClient
import com.example.edclfcm.api.FcmTokenRequest
import com.example.edclfcm.util.TokenManager
import com.google.firebase.messaging.FirebaseMessaging
import kotlinx.coroutines.launch
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts

@Composable
fun HomeScreen(onLogout: () -> Unit) {
    val context = LocalContext.current
    val coroutineScope = rememberCoroutineScope()
    val tokenManager = remember { TokenManager(context) }
    var fcmToken by remember { mutableStateOf(tokenManager.getFcmToken() ?: "Loading...") }
    var statusMessage by remember { mutableStateOf("") }

    val permissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestPermission(),
        onResult = { isGranted ->
            if (isGranted) {
                Log.d("FCM", "Notification permission granted")
            }
        }
    )

    LaunchedEffect(Unit) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            permissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
        }

        FirebaseMessaging.getInstance().token.addOnCompleteListener { task ->
            if (task.isSuccessful) {
                val token = task.result
                fcmToken = token
                tokenManager.saveFcmToken(token)

                // Send to backend
                coroutineScope.launch {
                    try {
                        val response = ApiClient.service.updateFcmToken(FcmTokenRequest(token))
                        if (response.isSuccessful) {
                            statusMessage = "FCM Token saved to backend."
                        } else {
                            statusMessage = "Failed to save to backend: ${response.code()}"
                        }
                    } catch (e: Exception) {
                        statusMessage = "Error: ${e.message}"
                    }
                }
            } else {
                fcmToken = "Failed to get token"
            }
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text("FCM Simulator", style = MaterialTheme.typography.headlineMedium)
        Spacer(modifier = Modifier.height(32.dp))

        Text("Your FCM Token:", style = MaterialTheme.typography.titleMedium)
        Spacer(modifier = Modifier.height(8.dp))
        
        val clipboardManager = androidx.compose.ui.platform.LocalClipboardManager.current
        
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                androidx.compose.foundation.text.selection.SelectionContainer(
                    modifier = Modifier.weight(1f).padding(start = 16.dp, top = 16.dp, bottom = 16.dp)
                ) {
                    Text(
                        text = fcmToken,
                        style = MaterialTheme.typography.bodySmall
                    )
                }
                TextButton(
                    onClick = {
                        clipboardManager.setText(androidx.compose.ui.text.AnnotatedString(fcmToken))
                        statusMessage = "FCM Token copied to clipboard!"
                    },
                    modifier = Modifier.padding(end = 8.dp)
                ) {
                    Text("Copy")
                }
            }
        }

        Spacer(modifier = Modifier.height(16.dp))
        Text(statusMessage, color = MaterialTheme.colorScheme.primary)

        Spacer(modifier = Modifier.height(48.dp))
        Button(
            onClick = {
                tokenManager.clear()
                onLogout()
            }
        ) {
            Text("Logout")
        }
    }
}
