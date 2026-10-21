import assert from "node:assert/strict";
import test from "node:test";
import { youtubeVideoId } from "../lib/campaign-content.ts";

test("allows supported YouTube URL formats", () => {
  for (const url of ["https://youtu.be/dQw4w9WgXcQ", "https://www.youtube.com/watch?v=dQw4w9WgXcQ", "https://youtube.com/shorts/dQw4w9WgXcQ", "https://youtube.com/embed/dQw4w9WgXcQ"])
    assert.equal(youtubeVideoId(url), "dQw4w9WgXcQ");
});
test("rejects unsafe and invalid embedded video URLs", () => {
  for (const url of ["javascript:alert(1)", "https://youtube.com.attacker.test/watch?v=dQw4w9WgXcQ", "https://youtube.com/watch?v=invalid", "https://user@youtube.com/watch?v=dQw4w9WgXcQ", "http://youtu.be/dQw4w9WgXcQ"])
    assert.equal(youtubeVideoId(url), null);
});
