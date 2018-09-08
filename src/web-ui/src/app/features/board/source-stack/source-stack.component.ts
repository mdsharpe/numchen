import { Component, OnInit, Input } from '@angular/core';
import { SourceStack } from 'app/shared/models';

@Component({
    selector: 'app-source-stack',
    templateUrl: './source-stack.component.html',
    styleUrls: ['./source-stack.component.scss']
})
export class SourceStackComponent implements OnInit {

    constructor() { }

    @Input() public stack: SourceStack | null = null;

    public ngOnInit() {
    }

}
