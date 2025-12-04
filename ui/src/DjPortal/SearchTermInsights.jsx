import { useState, useCallback } from 'react'
import { Button, Card, Form } from 'react-bootstrap';
import axios from 'axios';

function SearchTermInsights() {
    const [dayRange, setDayRange] = useState(7);
    const [insights, setInsights] = useState([]);

    // Debounce function
    const debounce = (func, delay) => {
        let debounceTimer;
        return function(...args) {
            const context = this;
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => func.apply(context, args), delay);
        };
    };

    // Handle track search with debounce
    const getSearchTermInsights = useCallback(
        debounce(async (selectedDayRange) => {
            setInsights([]);
        
            await axios.get(import.meta.env.VITE_APP_INSIGHTS_SEARCHTERMS, { params: { numberOfDays: selectedDayRange } })
                .then((response) => {
                    if (response.data && Array.isArray(response.data)){
                        setInsights(response.data);
                    } else {
                        console.error('Invalid search history data');
                    }
                },
                () => {
                    console.error('Error fetching search history');
                });
            }, 1000), []);

    const handleSelectDayRange = (event) => {
        setDayRange(event.target.value);
        getSearchTermInsights(event.target.value);
    };

    const handleGetInsightsClick = () => {
        getSearchTermInsights(dayRange);
    };

    const renderInsights = () => {
        return insights && insights.map((insightData, index) => {
          return (
            <tr key={index}>
                <td>{insightData.query}</td>
                <td>{insightData.uniqueCount}</td>
            </tr>
          )}
        )};

    return (
        <Card className='mb-3'>
            <Card.Header>User Search Terms</Card.Header>
            <Card.Body>
                <Form.Group className='mb-3'>
                    <Form.Label>Number of Days</Form.Label>
                    <Form.Range value={dayRange} onChange={handleSelectDayRange} min={1} max={30} />
                    <div className='form-text'>Show insights for the last {dayRange} days.</div>
                </Form.Group>
                <table className='table table-striped'>
                    <thead>
                        <tr>
                            <th>Search Term</th>
                            <th>Instances</th>
                        </tr>
                    </thead>
                    <tbody>
                        {renderInsights()}
                    </tbody>
                </table>
            </Card.Body>
            <Card.Footer>
                <Button variant='primary' onClick={handleGetInsightsClick}>Get Insights</Button>
            </Card.Footer>
        </Card>
    );
}

export default SearchTermInsights;