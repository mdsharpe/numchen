import { Component, ChangeDetectionStrategy, OnInit, OnDestroy } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { BoardFacadeService } from '../../board-facade.service';
import { GameViewModel } from '../../view-models';

@Component({
    selector: 'app-board',
    templateUrl: './board-shell.component.html',
    styleUrls: ['./board-shell.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [BoardFacadeService]
})
export class BoardShellComponent implements OnInit, OnDestroy {
    constructor(
        private readonly _service: BoardFacadeService
    ) {
    }

    private readonly unsubscribe$ = new Subject<void>();
    public board$: Observable<GameViewModel | null> | null = null;

    public ngOnInit(): void {
        this.board$ = this._service.board$
            .pipe(
                takeUntil(this.unsubscribe$)
            );
    }

    public ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    public columnAddClicked(columnIndex: number): void {
        this._service.moveNextToColumn(columnIndex);
    }

    public columnRemoveClicked(columnIndex: number) {
        this._service.moveLastToGoal(columnIndex);
    }
}
