import {fetchUser} from "../utils/user.js";
import {initCommentForm} from "./components/initCommentForm.js";
import {initCommentButtons} from "./components/initCommentButtons.js";

const dataLists = document.querySelectorAll('[data-component="data-list"]');
dataLists.forEach(initCommentForm);
fetchUser().then(_ => {
    dataLists.forEach(initCommentButtons);
});