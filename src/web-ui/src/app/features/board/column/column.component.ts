import { Component, OnInit, Input } from '@angular/core';
import { Column } from 'app/shared/models';

@Component({
    selector: 'app-column',
    templateUrl: './column.component.html',
    styleUrls: ['./column.component.css']
})
export class ColumnComponent implements OnInit {

    constructor() { }

    @Input() public column: Column | null = null;

    public ngOnInit() {
    }

}
