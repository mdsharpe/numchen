import { Component, OnInit, Input, ChangeDetectionStrategy } from '@angular/core';
import { GoalStackViewModel } from '../view-models/goal-stack.view-model';

@Component({
    selector: 'app-goal-stack',
    templateUrl: './goal-stack.component.html',
    styleUrls: ['./goal-stack.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class GoalStackComponent implements OnInit {

    constructor() { }

    @Input() public stack: GoalStackViewModel | null = null;

    public ngOnInit() {
    }

}
