// Returns the request's link only when it is safe to render as an href, otherwise null.
// The API already restricts stored links to http(s), but this is the last gate before the
// DOM and the data is user-supplied, so a javascript:/data: URL must not slip through.
export function getRequestLink(request) {
  const url = typeof request?.linkUrl === 'string' ? request.linkUrl.trim() : '';
  return /^https?:\/\//i.test(url) ? url : null;
}
