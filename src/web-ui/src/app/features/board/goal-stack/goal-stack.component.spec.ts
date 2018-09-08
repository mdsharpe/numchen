import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { GoalStackComponent } from './goal-stack.component';

describe('GoalStackComponent', () => {
  let component: GoalStackComponent;
  let fixture: ComponentFixture<GoalStackComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ GoalStackComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GoalStackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
