import { Card } from 'react-bootstrap';
import './EventSkeletonLoader.css';

function EventSkeletonLoader() {
  return (
    <Card className="mb-4 skeleton-card">
      <Card.Header className='fw-bold'>Loading...</Card.Header>
      <Card.Body>
        {/* Title skeleton */}
        <div className="skeleton-bar skeleton-title"></div>
        
        {/* Date skeleton */}
        <div className="skeleton-bar skeleton-date"></div>
        
        {/* Time skeleton */}
        <div className="skeleton-bar skeleton-time"></div>
        
        {/* Location name skeleton */}
        <div className="skeleton-bar skeleton-location"></div>
        
        {/* Location address skeleton */}
        <div className="skeleton-bar skeleton-address"></div>
        
        {/* Description skeleton - multiple lines */}
        <div className="skeleton-bar skeleton-description"></div>
        <div className="skeleton-bar skeleton-description-short"></div>
        
        {/* Tags skeleton */}
        <div className="skeleton-tags">
          <div className="skeleton-bar skeleton-tag"></div>
          <div className="skeleton-bar skeleton-tag"></div>
          <div className="skeleton-bar skeleton-tag"></div>
        </div>
      </Card.Body>
      
      <Card.Footer>
        <div className="skeleton-bar skeleton-button"></div>
      </Card.Footer>
    </Card>
  );
}

export default EventSkeletonLoader;
