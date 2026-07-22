import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SyncMonitoring } from './sync-monitoring';

describe('SyncMonitoring', () => {
  let component: SyncMonitoring;
  let fixture: ComponentFixture<SyncMonitoring>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SyncMonitoring],
    }).compileComponents();

    fixture = TestBed.createComponent(SyncMonitoring);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
