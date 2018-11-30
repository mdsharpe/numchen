import { Component, OnInit, HostBinding } from '@angular/core';

@Component({
    selector: 'app-menu',
    templateUrl: './menu.component.html',
    styleUrls: ['./menu.component.scss']
})
export class MenuComponent implements OnInit {
    constructor() { }

    @HostBinding('class.menu-open') private _open = false;

    public ngOnInit() {
    }

    public toggleMenu() {
        this._open = !this._open;
    }
}
