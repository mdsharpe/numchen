import { Component, ChangeDetectionStrategy, Input, Output, EventEmitter } from '@angular/core';
import { SourceStackViewModel } from 'app/features/board/view-models';

@Component({
    selector: 'app-source-stack',
    templateUrl: './source-stack.component.html',
    styleUrls: ['./source-stack.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SourceStackComponent {
    @Input() public stack: SourceStackViewModel | null = null;
    @Output() public readonly cardClick = new EventEmitter<void>();

    public cardClicked(cardIndex: number): void {
        if (this.stack && cardIndex === this.stack.cards.length - 1) {
            this.cardClick.emit();
        }
    }
}
