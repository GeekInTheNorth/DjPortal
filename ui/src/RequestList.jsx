import { useContext } from 'react'
import { Card, ListGroup } from 'react-bootstrap';
import { AppContext } from './AppContext.jsx';

function RequestList() {
  const { requestCollection } = useContext(AppContext);

  return (
    <Card className='my-3'>
      <Card.Header>Requested Tracks</Card.Header>
      <ListGroup variant="flush">
        {requestCollection && requestCollection.map((requestData, index) => {
          const isSpotifyUrl = typeof requestData.spotifyUrl === 'string' && requestData.spotifyUrl.startsWith('https://open.spotify.com/track/');
          return (
            <ListGroup.Item key={index} className="d-flex flex-column flex-md-row justify-content-between align-items-md-center">
                <div className='mt-2'>
                  <strong>Track:</strong> {requestData.trackName}
                  {isSpotifyUrl ? (<a href={requestData.spotifyUrl} className='spotify-link' target="_blank" rel="noopener noreferrer">Open in Spotify</a>) : ''}
                </div>
                <div className='mt-2'>
                  <strong>Requested By:</strong> {requestData.userName}
                </div>
            </ListGroup.Item>
          );
        })}
      </ListGroup>
    </Card>
  );
}

export default RequestList;