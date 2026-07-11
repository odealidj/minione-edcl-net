import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TruckAssignment } from './truck-assignment';

describe('TruckAssignment', () => {
  let component: TruckAssignment;
  let fixture: ComponentFixture<TruckAssignment>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TruckAssignment],
    }).compileComponents();

    fixture = TestBed.createComponent(TruckAssignment);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
