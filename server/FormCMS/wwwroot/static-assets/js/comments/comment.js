import {fetchUser} from "../utils/user.js";
import {initCommentForm} from "./components/initCommentForm.js";
import {initCommentButtons} from "./components/initCommentButtons.js";


export function renderComments(element, render) {
    const dataLists = element.querySelectorAll('[data-component="data-list"]');
    dataLists.forEach(list=>initCommentForm(list, render));
    fetchUser().then(_ => {
        dataLists.forEach(initCommentButtons);
    });
}