import { logout } from "../services/services.js";

export function loadNavBar(container) {
    const html = `
    <nav class="navbar navbar-expand-lg navbar-light bg-light">
        <a class="navbar-brand" href="/">
            <img alt="logo" src="img/fluent-cms.png" height="40" class="mr-2">
        </a>
        <div class="navbar-nav">
            <a class="nav-item nav-link border-item" href="./list.html">All Schemas</a>
            <a class="nav-item nav-link border-item" href="./list.html?type=entity">Entities</a>
            <a class="nav-item nav-link border-item" href="./list.html?type=query">Queries</a>
            <a class="nav-item nav-link border-item" href="./list.html?type=page">Pages</a>
            <a class="nav-item nav-link border-item" href="./edit.html?type=menu&name=top-menu-bar">MenuItems</a>
            <a class="nav-item nav-link border-item" href="../admin">Admin Panel</a>
            <a id="nav-item-exit" class="nav-item nav-link border-item" href="#">Exit</a>
        </div>
    </nav>`;

    if (typeof container === "string") {
        document.querySelector(container).innerHTML = html;
    } else if (container instanceof HTMLElement) {
        container.innerHTML = html;
    }

    const exitLink = document.getElementById("nav-item-exit");
    if (exitLink) {
        exitLink.addEventListener("click", function (e) {
            e.preventDefault();
            logout().then(() => {
                window.location.href = "/";
            });
        });
    }
}
