import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'not-found',
  //templateUrl: './not-found.component.html'
  template: `
<header class="intro-header">
  <div class="site-heading" style="display:table; margin: 0 auto">
    <div style="display:table-cell">
      <h1>404 - Page Not Found</h1>
      <span class="subheading">This isn't the page you are looking for.</span>
    </div>
  </div>
</header>
<div class="container">
  <div class="row">
    <div class="col-lg-8 col-lg-offset-2 col-md-10 col-md-offset-1">
      <span style="font-size:20px">Head back to <a href="/">the homepage</a></span>
    </div>
  </div>
</div>
`
  // styleUrls: ['./not-found.component.css']
})
export class NotFoundComponent implements OnInit {
  constructor() { }

  ngOnInit() { }
}
