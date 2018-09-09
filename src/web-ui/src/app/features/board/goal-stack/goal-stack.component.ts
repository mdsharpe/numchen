import { Component, OnInit, Input } from '@angular/core';
import { CardStack } from 'app/shared/models';

@Component({
    selector: 'app-goal-stack',
    templateUrl: './goal-stack.component.html',
    styleUrls: ['./goal-stack.component.css']
})
export class GoalStackComponent implements OnInit {

    constructor() { }

    @Input() public stack: CardStack | null = null;

    public ngOnInit() {
    }

}
