import { State, Action, StateContext, NgxsOnInit } from '@ngxs/store';
import produce from 'immer';
import { flow, range, last, isEmpty, find, some, } from 'lodash';
import { map, filter, isNil } from 'lodash/fp';

import { MoveNextToColumn, MoveLastToGoal, ResetBoard } from './board.actions';

export interface BoardStateModel {
    sourceStacks: number[][];
    columns: number[][];
    goalStacks: number[][];
    nextSourceValue: number | null;
}

@State<BoardStateModel>({
    name: 'board',
    defaults: {
        sourceStacks: [],
        columns: [],
        goalStacks: [],
        nextSourceValue: null
    }
})
export class BoardState implements NgxsOnInit {
    public ngxsOnInit(ctx: StateContext<BoardStateModel>): void {
        ctx.dispatch(new ResetBoard());
    }

    @Action(ResetBoard)
    public resetBoard(ctx: StateContext<BoardStateModel>): void {
        ctx.setState(
            flow(
                this.populateSourceStacks,
                this.populateColumns,
                this.populateGoalStacks,
                this.pickNextSource
            )({
                sourceStacks: [],
                columns: [],
                goalStacks: [],
                nextSourceValue: null
            })
        );
    }

    @Action(MoveNextToColumn)
    public moveNextToColumn(ctx: StateContext<BoardStateModel>, action: MoveNextToColumn): void {
        const board = ctx.getState();

        ctx.setState(flow(
            produce(draft => {
                const card = find(draft.sourceStacks, o => some(o, p => p === board.nextSourceValue)).pop();
                draft.columns[action.colIndex].push(card);
            }),
            this.pickNextSource
        )(board));
    }

    @Action(MoveLastToGoal)
    public moveLastToGoal(ctx: StateContext<BoardStateModel>, action: MoveLastToGoal): void {
        const board = ctx.getState();

        const card = last(board.columns[action.colIndex]);

        if (card) {
            const goalIndex = board.goalStacks.findIndex(
                o => card > 1 ? last(o) === card - 1 : isEmpty(o));

            if (goalIndex >= 0) {
                ctx.setState(produce(
                    board,
                    draft => {
                        draft.columns[action.colIndex].pop();
                        draft.goalStacks[goalIndex].push(card);
                    }));
            }
        }
    }

    private pickNextSource(board: BoardStateModel): BoardStateModel {
        const numbers = flow(
            map((o: number[]) => last(o)),
            filter<number>(o => !isNil(o))
        )(board.sourceStacks);

        const nextSourceValue =
            numbers.length > 0
                ? numbers[Math.floor(Math.random() * numbers.length)]
                : null;

        return produce(
            board,
            draft => {
                draft.nextSourceValue = nextSourceValue;
            });
    }

    private populateSourceStacks(board: BoardStateModel): BoardStateModel {
        return produce(
            board,
            draft => {
                draft.sourceStacks = [];
                range(0, 16).forEach(i => draft.sourceStacks.push(Array(6).fill(i + 1)));
            });
    }

    private populateColumns(board: BoardStateModel): BoardStateModel {
        return produce(
            board,
            draft => {
                draft.columns = [];
                range(0, 6).forEach(i => draft.columns.push([]));
            });
    }

    private populateGoalStacks(board: BoardStateModel): BoardStateModel {
        return produce(
            board,
            draft => {
                draft.goalStacks = [];
                range(0, 6).forEach(i => draft.goalStacks.push([]));
            });
    }
}
