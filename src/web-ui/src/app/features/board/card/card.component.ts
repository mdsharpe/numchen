import { Component, OnInit, Input, ChangeDetectionStrategy } from '@angular/core';
import { CardViewModel } from 'app/features/board/view-models';

@Component({
    selector: 'app-card',
    templateUrl: './card.component.html',
    styleUrls: ['./card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CardComponent implements OnInit {
    constructor() { }

    @Input() public card: CardViewModel;
    @Input() public moveable: boolean;

    public ngOnInit(): void {
    }

    public cardColor(): string {
        if (this.moveable) {
            return "";
        }

        const hue = ((this.card.value-1)/17) * 360;
        return "hsl(" + hue + ", 66%, 33%)";
    }
}
