import { Component, OnInit } from '@angular/core';
import { Game, SourceStack } from 'app/shared/models';
import { GameFactoryService } from 'app/shared/game-logic';

@Component({
    selector: 'app-board',
    templateUrl: './board.component.html',
    styleUrls: ['./board.component.scss']
})
export class BoardComponent implements OnInit {
    constructor(
        private readonly _gameFactory: GameFactoryService
    ) {
    }

    public game: Game | null = null;

    public ngOnInit(): void {
        this.game = this._gameFactory.create();
    }
}
