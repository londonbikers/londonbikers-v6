# londonbikers-v6
This is the sixth version of the website where we developed a forum from scratch without the editorial aspect. The goal was to focus on the community and all other parts of the old site were dropped, i.e. the editorial, galleries and directory. This resulted in a modern forum system that adopted a mobile-first design principle and had a fully-featured "Intercom" private messaging system.

The website worked well but it was clear that with a single person working on development we weren't going to be able to implement all the forum features we wanted and would end up lagging behind other open-source products that were gaining traction that we had originally discounted as not mature enough for our needs during the design phase. So after a few years the decision was made to migrate to Discourse, an open-source, modern discussion platform.

In hindsight, we believe this was a mistake, we should have modernised the main part of the site with the editorial and galleries, dropped off the directory and then migrated to the Discourse discussion platform to modernise the forums and enable a mobile-first experience. Losing the editorial aspect of the site cost the site a lot of traffic and discoverbility through the excellent Page Rankings we had. Though in fairness at the time part of the reason for focusing on the community was to better focus the resouces we had available.

The development stack was:
* ASP.NET MVC
* C#
* Signalr for real-time UI updates
* Knockout.js for front-end JS binding
* Lot's of supporting custom JavaScript
* SQL Server

## Design

<img src="https://londonbikersarchive.blob.core.windows.net/github/lbv6%20post%20button%20demos.png" width="500" />

<img src="https://londonbikersarchive.blob.core.windows.net/github/v6%20notifications-to-improve.png" alt="forum" width="500" />

<img src="https://londonbikersarchive.blob.core.windows.net/github/v6%20user%20profile%20v1.png" alt="user profile" width="500" />

<img src="https://londonbikersarchive.blob.core.windows.net/github/v6%20LB-Intercom-1.PNG" alt="Intercom messaging system" width="500" />
