import { Component, OnInit } from '@angular/core';
import { Game } from 'app/shared/models';
import { GameFactoryService, GameEngineService } from 'app/shared/game-logic';

@Component({
    selector: 'app-board',
    templateUrl: './board.component.html',
    styleUrls: ['./board.component.scss']
})
export class BoardComponent implements OnInit {
    constructor(
        private readonly _gameFactory: GameFactoryService,
        private readonly _gameEngine: GameEngineService
    ) {
    }

    public game: Game | undefined;

    public ngOnInit(): void {
        this.game = this._gameFactory.create();
        this._gameEngine.init(this.game);
        console.log(this.game);
    }
}
