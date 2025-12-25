import { useContext } from 'react'
import { AppContext } from './AppContext.jsx';
import EventSummary from './EventSummary.jsx'
import EventSkeletonLoader from './EventSkeletonLoader.jsx'
import Faq from './Faq.jsx'

function EventList() {

  const { eventCollection } = useContext(AppContext);

  const renderEventCollection = () => {
    // Show skeleton loaders while events are loading
    if (!eventCollection || eventCollection.length === 0) {
      return [0, 1, 2].map((index) => (
        <EventSkeletonLoader key={`skeleton-${index}`} />
      ));
    }

    return eventCollection.map((eventData) => {
      return (
        <EventSummary key={eventData.id} eventData={eventData} showRequestCta={true} />
      );
    });
  };

  return (
    <>
      {renderEventCollection()}
      <Faq durationHours={4} />
    </>
  );
}

export default EventList
