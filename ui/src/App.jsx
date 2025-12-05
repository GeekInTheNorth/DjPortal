import { useContext } from 'react'
import { AppContext } from './AppContext.jsx';
import EventDetails from './EventDetails.jsx'
import EventList from './EventList.jsx';

function App() {

  const { selectedView } = useContext(AppContext);

  return (
    <>
      { selectedView === 'list' ? <EventList /> : <EventDetails /> }
    </>
  )
}

export default App
