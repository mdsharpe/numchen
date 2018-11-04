import { Component, ChangeDetectionStrategy, Input, Output, EventEmitter } from '@angular/core';
import { ColumnViewModel } from 'app/features/board/view-models';

@Component({
    selector: 'app-column',
    templateUrl: './column.component.html',
    styleUrls: ['./column.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColumnComponent {
    @Input() public column: ColumnViewModel | null = null;
    @Output() public readonly addClick = new EventEmitter<void>();
    @Output() public readonly cardClick = new EventEmitter<void>();
    @Output() public readonly removeClick = new EventEmitter<void>();

    public addClicked(): void {
        this.addClick.emit();
    }

    public cardClicked(cardIndex: number): void {
        if (this.column && cardIndex === this.column.cards.length - 1) {
            this.cardClick.emit();
        }
    }

    public removeClicked(): void {
        this.removeClick.emit();
    }
}
