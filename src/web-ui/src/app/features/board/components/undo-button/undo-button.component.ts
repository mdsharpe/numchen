import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
    selector: 'app-undo-button',
    templateUrl: './undo-button.component.html',
    styleUrls: ['./undo-button.component.scss']
})
export class UndoButtonComponent {
    @Input() public canUndo: boolean | null = true;
    @Output() public readonly undoClick = new EventEmitter<void>();

    public undoClicked(): void {
        if (this.canUndo) {
            this.undoClick.emit();
        }
    }
}
