export class MoveNextToColumn {
    static readonly type = '[Board] Move next to column';
    constructor(public colIndex: number) {
    }
}

export class MoveLastToGoal {
    static readonly type = '[Board] Move last to goal';
    constructor(public colIndex: number) {
    }
}

export class ResetBoard {
    static readonly type = '[Board] Reset';
}
