// Photo Reviewer 4Net (C) 2025 Karl Pickett
const dom = {}
dom.main = document.getElementById("main");
dom.listing_window = document.getElementById("listing_window");
dom.listing_files_ul = document.getElementById("listing_files_ul");
dom.listing_categories_ul = document.getElementById("listing_categories_ul");
dom.media_window = document.getElementById("media_window");
dom.media_window_image = document.getElementById("media_window_image");
dom.media_window_video = document.getElementById("media_window_video");
dom.media_window_ratings_ul = document.getElementById("media_window_ratings_ul");
dom.media_window_error = document.getElementById("media_window_error");
dom.download_file_list = document.getElementById("download_file_list");

let g_media_list = null;       // All files returned from server, never category filtered

// We have two timers, because we hide the control buttons faster if the mouse is not over them.
let g_mousemove_timeout = 150;
let g_hover_timeout     = 2000;
let g_mousemove_timer = null;
let g_hover_timer = null;


//
// Simple functions first, for a foundation.
// If we don't start with something simple, we'll never end there.
//

// This can return null, if the media window for 'unrated' closes and all files are now rated
function TryGetSelectedFileListRow() {
    return dom.listing_files_ul.querySelector(`li[data-selected="1"]`);
}

function _GetSelectedFileListRow() {
    let ret = TryGetSelectedFileListRow();
    if (ret == null) {
        throw new Error("no file is currently selected");
    }
    return ret;
}

function GetMediaEntryForRow(li) {
    return g_media_list[li.dataset.masterIndex];
}

function AdvanceFileList(li, delta) {
    return delta < 0 ? li.previousSibling : li.nextSibling;
}

// One of the 3 rating buttons
function GetRatingButton(rating) {
    return dom.media_window_ratings_ul.querySelector(`li[data-value="${rating}"]`)
}

// One of 3 ratings + 3 categories
function GetCurrentFilter() {
    return dom.listing_categories_ul.querySelector("li[data-selected]").dataset.value;
}

function ClearVideoElement() {
    dom.media_window_video.innerHTML = '';
}

function IsMediaWindowVisible() {
    return dom.media_window.dataset.visible === "1";
}

function ToggleFullScreen() {
    if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen();
    } else if (document.exitFullscreen) {
        document.exitFullscreen();
    }
}

function IsTouchDevice() {
    return window.matchMedia("(any-hover: none)").matches;
}

function EscapeHTML(s) {
  return s.replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
}

function RemoveDatasetValueFromChildren(element, name) {
    for (let node of element.children) {
        if (node.dataset[name] !== undefined) {
            delete node.dataset[name]
        }
    }
}

function RatingMatchesFilter(rating, filter) {
    return (filter == "Everything" || (rating == filter)
            || (rating != "" && filter == "Rated")
            || (rating == "" && filter == "Unrated"))
}

function ToggleVideoPaused() {
    // Don't mess with ipad controls, let it do its own thing.
    // Firefox also never fires a click event, only Chrome does.
    // Web standards = LMAO
    let vid = dom.media_window_video.querySelector("video")
    if (vid != null && !IsTouchDevice()) {
        if (vid.paused) {
            console.log("playing")
            vid.play()
        } else {
            console.log("pausing")
            vid.pause()
        }
    }
}

function KickMouseTimer() {
    clearTimeout(g_mousemove_timer)
    clearTimeout(g_hover_timer)
    dom.main.dataset.mouseActive = "1";
    dom.main.dataset.mouseActive2 = "1";
    g_mousemove_timer = setTimeout(() => {
        dom.main.dataset.mouseActive = "0";
    }, g_mousemove_timeout);
    g_hover_timer = setTimeout(() => {
        dom.main.dataset.mouseActive2 = "0";
    }, g_hover_timeout);
}

function Sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function SetDarkMode(value) {
    if (value) {
        dom.main.classList.add("dark-mode")
    } else {
        dom.main.classList.remove("dark-mode")
    }
    document.querySelector("#input_dark_mode").checked = value;
    localStorage.setItem("PhotoReviewer4Net.isDark", value ? "1" : "0");
}

function SetAutoAdvance(value) {
    document.querySelector("#input_auto_advance").checked = value;
    localStorage.setItem("PhotoReviewer4Net.isAutoAdvance", value ? "1" : "0");
}

function LoadSettings() {
    const isDark = localStorage.getItem("PhotoReviewer4Net.isDark");
    SetDarkMode(isDark === "1");
    const isAutoAdvance = localStorage.getItem("PhotoReviewer4Net.isAutoAdvance");
    SetAutoAdvance(isAutoAdvance === "1");
}



//
// More complex functions
//

async function TryLoadMediaList() {
    console.log("TryLoadMediaList()");
    let response;
    try {
        response = await fetch("/api/GetMediaList");
        if (!response.ok) {
            throw new Error("http error code")
        }
    } catch (e) {
        alert(`Error calling /api/GetMediaList.  Is the server running and reachable?`);
        return false;
    }

    g_media_list = await response.json();
    return true;
}


async function ShowMediaFile(li) {
    if (li == null) {
        console.log("null file");
        return;
    }

    if (!TryStartAction("ShowMediaFile")) {
        return;
    }

    // Show window, if it's not already.  We load it on top of the listings, for performance.
    // (The listings don't reflow when window closes)
    dom.media_window.dataset.visible = "1";

    // Hide any previous error window
    dom.media_window_error.dataset.visible = "0";

    // Update the selected table row
    RemoveDatasetValueFromChildren(dom.listing_files_ul, "selected");
    li.dataset.selected = "1";

    // Update the rating buttons
    RemoveDatasetValueFromChildren(dom.media_window_ratings_ul, "selected");
    let entry = GetMediaEntryForRow(li);
    if (entry.Rating != "") {
        let rating_li = GetRatingButton(entry.Rating)
        rating_li.dataset.selected = "1";
    }

    // Disable prev/next if we are at the end
    document.getElementById("media_window_controls_previous").dataset.disabled =
            li.previousSibling == null ? "1" : "0";
    document.getElementById("media_window_controls_next").dataset.disabled =
            li.nextSibling == null ? "1" : "0";


    // Start loading the new image or video.
    const media_stream =  `/api/GetMediaStream?filePath=${encodeURIComponent(entry.FilePath)}`;
    if (entry.FileType == "IMAGE") {
        dom.media_window_image.src = media_stream;
        dom.media_window_image.style.display = "block";
        dom.media_window_video.style.display = "none";
        ClearVideoElement();
    } else {
        // The reason we create a new video tag each time, is we want an empty .dataset, so we always know
        // when the _first_ load or error event was sent for this src.  Otherwise, there are races with other user actions.
        dom.media_window_video.innerHTML = "<video autoplay controls controlslist='nofullscreen' disablepictureinpicture></video>";
        const vid = dom.media_window_video.querySelector("video");
        console.assert(vid != null);
        console.assert(vid.dataset.signaled === undefined);
        vid.addEventListener('canplay', (event) => MediaLoadEvent(event));
        vid.addEventListener('error', (event) => MediaLoadEvent(event));

        vid.src = media_stream;
        vid.play();
        vid.focus(); // try to bandaid spacebar problems

        dom.media_window_image.style.display = "none";
        dom.media_window_video.style.display = "block";
    }
}


function MediaLoadEvent(event) {
    console.log(`MediaLoadEvent() ${event.type} from ${event.target.tagName}`);
    console.log(event);

    // Videos are tricky because they send multiple events, unlike images.
    // Ignore a video error that comes in after a canplay event, or secondary
    // canplay events (Which are fired when seeking).
    // We'll just keep the native video tag up.  The error might be recoverable
    // if the user clicks play again, or seeks.
    if (event.target.tagName == "VIDEO") {
        if (event.target.dataset.signaled !== undefined) {
            console.log("Video previously told us it was ok, so talk to the hand")
            return;
        }
        event.target.dataset.signaled = "1";
    }

    // Both img and video use the 'error' type.
    if (event.type == "error") {
        let file_li = _GetSelectedFileListRow();
        let entry = GetMediaEntryForRow(file_li);
        dom.media_window_error.dataset.visible = "1";
        dom.media_window_error.innerHTML = `Media file failed to load:<br> ${EscapeHTML(entry.DisplayName)}`;
    }

    EndAction("media load is no longer pending");
}


async function SetRating(newRating) {
    if (!IsMediaWindowVisible()) {
        console.log("media window is not active");
        return;
    }

    // Preflight checks ok, do it, as long as another action isn't running.
    if (!TryStartAction("SetRating")) {
        return;
    }

    // For simplicity, we don't allow a rating to be removed via the the main
    // control buttons.  It causes major confusion with autoadvance.

    let file_li = _GetSelectedFileListRow();
    let entry = GetMediaEntryForRow(file_li);
    let button = GetRatingButton(newRating);

    // Send API Request
    let body = { "FilePath": entry.FilePath, Rating: newRating }
    let response;
    try {
        response = await fetch("/api/SetRating", {
            method: "POST",
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(body)
        });
        if (!response.ok) {
            throw new Error("http error code")
        }
    } catch (e) {
        alert(`Error calling /api/SetRating.  Is the server running and reachable?`);
        EndAction("server failed");
        return;
    }

    // Update master data
    entry.Rating = newRating;

    // Update UI DOM - Rating Button
    RemoveDatasetValueFromChildren(dom.media_window_ratings_ul, "selected");
    button.dataset.selected = "1";

    // Update UI DOM - File List Icon
    file_li.querySelector(".icon").dataset.value = newRating;

    EndAction("rating done");

    // This may trigger a second action
    if (document.querySelector("#input_auto_advance").checked) {
        ShowMediaFile(AdvanceFileList(file_li, +1));
    }
}


function CloseMediaWindow() {
    if (IsActionInProgress()) {
        console.log("sorry, not closing while an async operation is pending");
        return;
    }

    ClearVideoElement(); // way easier than trying to remove event listeners manually
    dom.media_window.dataset.visible = "0";

    // Remove files from DOM table that no longer apply to the current filter.
    // Note that we don't want to do this until they closed the media window,
    // because they might change their mind and go backwards and change the rating.
    //
    const filter = GetCurrentFilter();
    console.log(`current filter is ${filter}`)
    let nodes_to_remove = []
    for (let n of dom.listing_files_ul.children) {
        //console.log(n)
        let entry = GetMediaEntryForRow(n);
        if (!RatingMatchesFilter(entry.Rating, filter)) {
            console.log(`removeChild due to rating '${entry.Rating}'`)
            nodes_to_remove.push(n)
        }
    }
    for (let n of nodes_to_remove) {
        dom.listing_files_ul.removeChild(n);
    }

    // Update counts
    UpdateCategoryCounts();

    // Scroll to selected file list row (if one exists; it might no longer exist for the current filter)
    let selected_node = TryGetSelectedFileListRow();
    if (selected_node !== null) {
        console.log("scrolling to selected file");
        selected_node.scrollIntoView({ behavior: "smooth", block: "center" });
    } else {
        console.log("no file is currently selected")
    }
}


function ShowCategory(li) {
    // Update the selected button
    RemoveDatasetValueFromChildren(dom.listing_categories_ul, "selected");
    li.dataset.selected = "1";

    // Clear the file list table, and build its DOM from scratch.
    // Note: this clears any selected row, which also causes headaches for keyboard navigation.
    // (Which is the main reason I'm punting on implementing that for the table)
    //
    const filter = GetCurrentFilter();
    let s = ""
    for (let i = 0; i < g_media_list.length; i++) {
        let entry = g_media_list[i];
        if (RatingMatchesFilter(entry.Rating, filter)) {
            s += `<li class="datarow" data-master-index="${i}">${EscapeHTML(entry.DisplayName)}<span class="icon" data-value="${entry.Rating}"></span></li>`;
        }
    }
    dom.listing_files_ul.innerHTML = s;

    dom.download_file_list.href = `/api/DownloadFileList?ratingFilter=${filter}`;
    dom.download_file_list.innerHTML = `Download file names as a text file`
}


function UpdateCategoryCounts() {
    let category_map = {}
    for (let li of dom.listing_categories_ul.querySelectorAll("li")) {
        console.log(`Updating counts for ${li.dataset.value}`)
        category_map[li.dataset.value] = { li: li, count: 0 }
    }
    console.log(category_map);
    for (let entry of g_media_list) {
        let rating = entry.Rating;
        if (rating == "") {
            category_map.Unrated.count += 1;
        } else {
            category_map.Rated.count += 1;
        }

        if (rating == "Good" || rating == "Bad" || rating == "Unsure") {
            category_map[rating].count += 1;
        }

        category_map["Everything"].count += 1;
    }
    for (const [category, data] of Object.entries(category_map)) {
        //console.log(category, data);
        const span = data.li.querySelector("span.count");
        span.innerText = data.count;
    }
}


function AddEventListeners() {
    document.getElementById("media_window_controls_close").addEventListener("click", () => {
        CloseMediaWindow();
    });
    document.getElementById("media_window_controls_previous").addEventListener("click", () => {
        KickMouseTimer();
        ShowMediaFile(AdvanceFileList(_GetSelectedFileListRow(), -1));
    });
    document.getElementById("media_window_controls_next").addEventListener("click", () => {
        KickMouseTimer();
        ShowMediaFile(AdvanceFileList(_GetSelectedFileListRow(), +1));
    });
    document.getElementById("media_window_controls_good").addEventListener("click", () => {
        KickMouseTimer();
        SetRating("Good");
    });
    document.getElementById("media_window_controls_unsure").addEventListener("click", () => {
        KickMouseTimer();
        SetRating("Unsure");
    });
    document.getElementById("media_window_controls_bad").addEventListener("click", () => {
        KickMouseTimer();
        SetRating("Bad");
    });

    dom.listing_categories_ul.addEventListener("click", (event) => {
        let li = event.target;
        while (li.nodeName != "LI") { li = li.parentNode; }
        ShowCategory(li);
    });

    // Set one listener for the entire UL, so we don't have the overhead of
    // maintaining listeners for each LI, especially when they are cleared/recreated.
    dom.listing_files_ul.addEventListener("click", (event) => {
        for (let n = event.target; n !== dom.listing_files_ul; n = n.parentNode) {
            if (n.nodeName == "LI") {
                ShowMediaFile(n);
                break;
            }
        }
    });

    document.documentElement.addEventListener("keydown", (event) => {
        //console.log(event);
        if (event.ctrlKey || event.metaKey || event.shiftKey) {
            return;
        }

        if (event.key == 't') {
            //dom.main.classList.toggle("dark-mode");
        } else if (event.key == 'g') {
            SetRating("Good");
        } else if (event.key == 's') {
            SetRating("Unsure");
        } else if (event.key == 'b') {
            SetRating("Bad");
        } else if (event.key == 'f') {
            ToggleFullScreen();
        } else if (event.key == 'ArrowLeft' && IsMediaWindowVisible()) {
            ShowMediaFile(AdvanceFileList(_GetSelectedFileListRow(), -1));
        } else if (event.key == 'ArrowRight' && IsMediaWindowVisible()) {
            ShowMediaFile(AdvanceFileList(_GetSelectedFileListRow(), +1));
        } else if (event.key == 'q' && IsMediaWindowVisible()) {
            CloseMediaWindow();
        }
    })
    document.documentElement.addEventListener("mousemove", (event) => {
        KickMouseTimer();
    })
    document.querySelector("#input_auto_advance").addEventListener("change", function()  {
        SetAutoAdvance(this.checked);
    })
    document.querySelector("#input_dark_mode").addEventListener("change", function()  {
        SetDarkMode(this.checked);
    })
    dom.media_window_image.addEventListener("dblclick", function() {
        ToggleFullScreen();
    });
    dom.media_window_video.addEventListener("dblclick", function() {
        ToggleFullScreen();
    });
    dom.media_window_video.addEventListener("click", function(event) {
        console.log(event);
        ToggleVideoPaused();
    });

    dom.media_window_image.addEventListener('load', (event) => MediaLoadEvent(event));
    dom.media_window_image.addEventListener('error', (event) => MediaLoadEvent(event));
}


// Only one user action can run at a time.
// This is called by 1) Loading a new image, 2) Rating an image.
// Note that when 'auto advance' is enabled, there are still two separate,
// sequential actions (rating + loading).

function TryStartAction(message) {
    if (IsActionInProgress()) {
        return false;
    }
    console.log(`StartAction() - ${message}`)
    dom.main.dataset.activeAction = "1";
    return true;
}

function EndAction(message) {
    console.log(`EndAction() - ${message}`)
    dom.main.dataset.activeAction = "0";
}

function IsActionInProgress() {
    return (dom.main.dataset.activeAction == "1")
}


// Main entry point
async function main() {
    LoadSettings();
    if (IsTouchDevice()) {
        // Tablets are not what this app was designed for, but they sort-of work.
        console.log("touch/tablet detected, using longer hover timeout");
        g_mousemove_timeout = 3000;
    }
    AddEventListeners();
    if (await TryLoadMediaList()) {
        dom.listing_window.dataset.visible = "1";
        UpdateCategoryCounts();
        ShowCategory(listing_categories_ul.querySelector(`li[data-value="Everything"]`));
    }
    console.log('Startup complete');
}

await main();
