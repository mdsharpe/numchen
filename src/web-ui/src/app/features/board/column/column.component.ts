import { Component, OnInit, Input, ChangeDetectionStrategy, Output, EventEmitter } from '@angular/core';
import { ColumnViewModel } from '../view-models/column.view-model';

@Component({
    selector: 'app-column',
    templateUrl: './column.component.html',
    styleUrls: ['./column.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColumnComponent implements OnInit {

    constructor() { }

    @Input() public column: ColumnViewModel | null = null;

    @Output() public readonly addClick = new EventEmitter<void>();
    @Output() public readonly removeClick = new EventEmitter<void>();

    public ngOnInit(): void {
    }

    public addClicked(): void {
        this.addClick.emit();
    }

    public removeClicked(): void {
        this.removeClick.emit();
    }
}
