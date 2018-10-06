import { Component, OnInit, Input, ChangeDetectionStrategy, Output, EventEmitter } from '@angular/core';
import { CardStack } from 'app/shared/models';

@Component({
    selector: 'app-column',
    templateUrl: './column.component.html',
    styleUrls: ['./column.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColumnComponent implements OnInit {

    constructor() { }

    @Input() public column: CardStack | null = null;

    @Output() public readonly addClick = new EventEmitter<void>();

    public ngOnInit(): void {
    }

    public addClicked():void {
        this.addClick.emit();
    }
}
