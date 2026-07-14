import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DeliveryMonitoring } from './delivery-monitoring';

describe('DeliveryMonitoring', () => {
  let component: DeliveryMonitoring;
  let fixture: ComponentFixture<DeliveryMonitoring>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DeliveryMonitoring],
    }).compileComponents();

    fixture = TestBed.createComponent(DeliveryMonitoring);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
