import { useEffect, useState } from 'react'
import { Card, Accordion } from 'react-bootstrap';
import PropTypes from 'prop-types';

function Faq(props) {

    const [durationHours, setDurationHours] = useState(4);
    const [upperRequestCount, setUpperRequestCount] = useState(0);
    const [lowerRequestCount, setLowerRequestCount] = useState(0);

    useEffect(() => {  
        const providedHours = props.durationHours ?? 4;

        if (providedHours > 0) {
            setDurationHours(providedHours);
            setUpperRequestCount(providedHours * 4);
            setLowerRequestCount(providedHours * 2);
        } else {
            setDurationHours(4);
            setUpperRequestCount(16);
            setLowerRequestCount(2);
        }
    }, [])

    return(
        <Card className='mb-3'>
            <Card.Header>FAQ</Card.Header>
            <Card.Body>
                <Accordion>
                    <Accordion.Item eventKey="0">
                        <Accordion.Header>Will My Request Get Played?</Accordion.Header>
                        <Accordion.Body>
                            <p>Requests will be played on a best endevours basis.  Just because a request is made, I can not promise that it will be played.</p>
                            <p>In order to get played, a track must have a relatively stable beat between 110 and 150 beats per minute and must not be excessively offensive. Over the course of a {durationHours} hour freestyle, I will planning to play between {lowerRequestCount} and {upperRequestCount} requests and work to fit them into one of {durationHours} journeys.  If there are too many requests of a similar speed, then a selection will be made.</p>
                        </Accordion.Body>
                    </Accordion.Item>
                    <Accordion.Item eventKey="1">
                        <Accordion.Header>What if you don&apos;t have my song?</Accordion.Header>
                        <Accordion.Body>
                            <p>If I do not have the song you have requested, I will preview that song online and if it matches our criteria, then I will attempt to purchase that song so I can play it.</p>
                        </Accordion.Body>
                    </Accordion.Item>
                    <Accordion.Item eventKey="2">
                        <Accordion.Header>Can I request more than one track?</Accordion.Header>
                        <Accordion.Body>
                            <p>Yes, but in order to be fair to other dancers, your requests will be treated as options and I will attempt to honour just one of them.</p>
                        </Accordion.Body>
                    </Accordion.Item>
                    <Accordion.Item eventKey="3">
                        <Accordion.Header>Why are you buying tracks instead of streaming them?</Accordion.Header>
                        <Accordion.Body>
                            <p>Artists make very little from streaming services and content on streaming services is not guaranteed to always be on that service.  By purchasing a track, I support the artist and ensure that I have access to that track indefinitely.</p>
                        </Accordion.Body>
                    </Accordion.Item>
                    <Accordion.Item eventKey="4">
                        <Accordion.Header>Who Are These Events For?</Accordion.Header>
                        <Accordion.Body>
                            <p>These Events are aimed at people who are into social partner dancing. Our specific style is known as Ceroc, but also commonly as Modern Jive.</p>
                        </Accordion.Body>
                    </Accordion.Item>
                    <Accordion.Item eventKey="5">
                        <Accordion.Header>Where Can I Learn To Dance?</Accordion.Header>
                        <Accordion.Body>
                            <p><a href="https://www.facebook.com/CerocYorkshire" target="_blank" rel="nofollow">Ceroc Yorkshire</a> host 3 regular class nights every week across the West Yorkshire region at venues in Tadcaster, Leeds and Ilkley. Doors open at 7:30pm, the beginners class starts at 7:45pm and the intermediate class starts at 9:00pm.</p>
                            <p>For more information, do checkout the <a href="https://www.facebook.com/CerocYorkshire" target="_blank" rel="nofollow">Ceroc Yorkshire</a> facebook page as well as the national <a href='https://www.ceroc.com/' rel='nofollow'>Ceroc</a> website.</p>
                        </Accordion.Body>
                    </Accordion.Item>
                    <Accordion.Item eventKey="6">
                        <Accordion.Header>Does This Website Use Cookies</Accordion.Header>
                        <Accordion.Body>
                            <p>This website uses a single essential cookie to ensure it functions properly. This cookie does not collect personal data and is not used for advertising or analytics.</p>
                            <p>By continuing to use this site, you agree to the use of this essential cookie.</p>
                            <p>The cookie is called <em>cydjr.requestor</em>. It is used to link you to requests you have made and contains only a randomly generated identifier. The cookie expires after 365 days.</p>
                        </Accordion.Body>
                    </Accordion.Item>
                </Accordion>
            </Card.Body>
        </Card>)
}

Faq.propTypes = {
    durationHours: PropTypes.number,
};

export default Faq