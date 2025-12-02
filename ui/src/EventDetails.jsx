import { useContext} from 'react'
import { AppContext } from './AppContext.jsx';
import EventSummary from './EventSummary.jsx'
import Faq from './Faq.jsx'
import RequestForm from './RequestForm.jsx';
import RequestList from './RequestList.jsx';

function EventDetails() {

  const { selectedEvent } = useContext(AppContext);

    return (
    <>
        <EventSummary eventData={selectedEvent} showRequestCta={false} />
        { selectedEvent.isRequestable ? <RequestForm /> : null }
        { selectedEvent.isRequestable ? <RequestList /> : null }
        <Faq durationHours={selectedEvent.durationHours} />
    </>
  )
}

export default EventDetails
