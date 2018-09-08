import { SourceStack, Column, GoalStack } from 'app/shared/models';

export class Game {
    public readonly sourceStacks : SourceStack[] = [];
    public readonly columns : Column[] = [];
    public readonly goalStacks : GoalStack[] = [];
}
