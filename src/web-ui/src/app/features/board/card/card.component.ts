import { Component, OnInit, Input } from '@angular/core';
import { Card } from 'app/shared/models';

@Component({
    selector: 'app-card',
    templateUrl: './card.component.html',
    styleUrls: ['./card.component.scss']
})
export class CardComponent implements OnInit {

    constructor() { }

    @Input() public card: Card | null = null;

    public ngOnInit() {
    }

}
