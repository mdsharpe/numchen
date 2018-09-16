import { Component, OnInit, Input, ChangeDetectionStrategy } from '@angular/core';
import { CardStack } from 'app/shared/models';

@Component({
    selector: 'app-column',
    templateUrl: './column.component.html',
    styleUrls: ['./column.component.css'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColumnComponent implements OnInit {

    constructor() { }

    @Input() public column: CardStack | null = null;

    public ngOnInit() {
    }

}
