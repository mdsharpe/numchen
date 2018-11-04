import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CardViewModel } from 'app/features/board/view-models';

@Component({
    selector: 'app-card',
    templateUrl: './card.component.html',
    styleUrls: ['./card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CardComponent {
    @Input() public card: CardViewModel | null = null;
    @Input() public raised: boolean | null = null;
    @Input() public highlighted: boolean | null = null;
    @Input() public clickable: boolean | null = null;

    public cardColor(): string {
        if (this.highlighted || !this.card) {
            return '';
        }

        const hue = ((this.card.value - 1) / 17) * 360;
        return 'hsl(' + hue + ', 66%, 33%)';
    }
}
