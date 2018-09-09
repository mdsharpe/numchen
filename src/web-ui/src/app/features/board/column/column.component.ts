import { Component, OnInit, Input } from '@angular/core';
import { CardStack } from 'app/shared/models';

@Component({
    selector: 'app-column',
    templateUrl: './column.component.html',
    styleUrls: ['./column.component.css']
})
export class ColumnComponent implements OnInit {

    constructor() { }

    @Input() public column: CardStack | null = null;

    public ngOnInit() {
    }

}
