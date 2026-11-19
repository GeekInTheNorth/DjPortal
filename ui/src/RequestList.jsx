import { useContext } from 'react'
import { Card, ListGroup } from 'react-bootstrap';
import { AppContext } from './AppContext.jsx';
import { getRequestLink } from './requestLink.js';

function RequestList() {
  const { requestCollection, requestsLoaded } = useContext(AppContext);
  const hasNoRequests = requestsLoaded && (!requestCollection || requestCollection.length === 0);

  return (
    <Card className='my-3'>
      <Card.Header>Requested Tracks</Card.Header>
      <ListGroup variant="flush">
        {hasNoRequests && (
          <ListGroup.Item className='text-muted text-center py-3'>
            No tracks requested yet. Be the first to make a request!
          </ListGroup.Item>
        )}
        {requestCollection && requestCollection.map((requestData, index) => {
          const linkUrl = getRequestLink(requestData);
          return (
            <ListGroup.Item key={index} className="d-flex flex-column flex-md-row justify-content-between align-items-md-center">
                <div className='mt-2'>
                  {linkUrl
                    ? (<a href={linkUrl} className='request-link' target="_blank" rel="noopener noreferrer">{requestData.trackName}</a>)
                    : (<><strong>Track:</strong> {requestData.trackName}</>)}
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