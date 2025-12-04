import { useContext } from 'react'
import { AppContext } from './AppContext.jsx';
import EventSummary from './EventSummary.jsx'
import Faq from './Faq.jsx'

function EventList() {

  const { eventCollection } = useContext(AppContext);

  const renderEventCollection = () => {
    return eventCollection && eventCollection.map((eventData) => {
      return (
        <EventSummary key={eventData.id} eventData={eventData} showRequestCta={true} />
      )}
    )}

    return (
    <>
      {renderEventCollection()}
      <Faq durationHours={4} />
    </>
  )
}

export default EventList
