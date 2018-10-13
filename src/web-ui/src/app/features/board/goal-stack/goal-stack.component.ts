import { Component, OnInit, Input, ChangeDetectionStrategy } from '@angular/core';
import { CardStack } from 'app/shared/models';

@Component({
    selector: 'app-goal-stack',
    templateUrl: './goal-stack.component.html',
    styleUrls: ['./goal-stack.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class GoalStackComponent implements OnInit {

    constructor() { }

    @Input() public stack: CardStack | null = null;

    public ngOnInit() {
    }

}
