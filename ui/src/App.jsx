import { useContext, useEffect, useState } from 'react'
import { AppContext } from './AppContext.jsx';
import EventDetails from './EventDetails.jsx'
import EventList from './EventList.jsx';
import { Alert } from 'react-bootstrap';

function App() {

  const { selectedView } = useContext(AppContext);
  const [sharedData, setSharedData] = useState(null);
  const [showSharedAlert, setShowSharedAlert] = useState(false);

  useEffect(() => {
    // Check if app was opened via share target
    const urlParams = new URLSearchParams(window.location.search);
    
    if (urlParams.get('shared') === 'true') {
      const url = urlParams.get('url');
      const title = urlParams.get('title');
      const text = urlParams.get('text');
      
      setSharedData({ url, title, text });
      setShowSharedAlert(true);
      
      // Clean up URL without reloading
      window.history.replaceState({}, '', '/');
      
      // Auto-hide alert after 5 seconds
      setTimeout(() => setShowSharedAlert(false), 5000);
    } else if (urlParams.get('shared') === 'error') {
      setShowSharedAlert(true);
      window.history.replaceState({}, '', '/');
      setTimeout(() => setShowSharedAlert(false), 5000);
    }
  }, []);

  return (
    <>
      {showSharedAlert && sharedData && (
        <Alert variant="info" onClose={() => setShowSharedAlert(false)} dismissible className="m-3">
          <Alert.Heading>Shared Content Received</Alert.Heading>
          <p>URL: {sharedData.url}</p>
          {sharedData.title && <p>Title: {sharedData.title}</p>}
          {sharedData.text && <p>Text: {sharedData.text}</p>}
          <hr />
          <p className="mb-0">
            You can now use this URL to make a music request. Copy the song details and paste them into the request form.
          </p>
        </Alert>
      )}
      {showSharedAlert && !sharedData && (
        <Alert variant="danger" onClose={() => setShowSharedAlert(false)} dismissible className="m-3">
          <Alert.Heading>Share Failed</Alert.Heading>
          <p>There was an error processing the shared content.</p>
        </Alert>
      )}
      { selectedView === 'list' ? <EventList /> : <EventDetails /> }
    </>
  )
}

export default App
