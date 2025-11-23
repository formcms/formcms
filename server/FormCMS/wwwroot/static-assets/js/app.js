import {trackVisit} from "./services/engagementService.js";
import {renderEngagementBar} from "./engagements/engagement.js";
import {loadCookieBanner} from "./cookies/cookies.js";
import {renderComments} from "./comments/comment.js";
import {formatHtmlElement} from "./formatter/formatter.js";
import {renderPagination} from "./pagination/pagination.js";
import {initPlayer} from "./video/video.js";
import {renderNotifications} from "./notifications/notification.js";
import {renderAvatar} from "./useAvatar/userAvatar.js";

trackVisit();
renderNotifications();
renderAvatar();
loadCookieBanner()

render(document);
function render(ele){
    renderEngagementBar(ele);
    renderComments(ele, render);
    formatHtmlElement(ele);
    renderPagination(ele, render);
    initPlayer(ele);
}