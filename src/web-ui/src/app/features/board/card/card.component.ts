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
}
