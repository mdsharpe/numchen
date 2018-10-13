import { Component, OnInit, Input, ChangeDetectionStrategy } from '@angular/core';
import { SourceStackViewModel } from 'app/features/board/view-models';

@Component({
    selector: 'app-source-stack',
    templateUrl: './source-stack.component.html',
    styleUrls: ['./source-stack.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SourceStackComponent implements OnInit {

    constructor() { }

    @Input() public stack: SourceStackViewModel | null = null;

    public ngOnInit() {
    }
}
