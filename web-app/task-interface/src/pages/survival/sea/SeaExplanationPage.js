// ExplanationPage.js
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import '../css/SurvivalExplanationPage.css';

function SeaExplanationPage() {
  const navigate = useNavigate();
  const [objects] = useState(
    ['sextant', 'shaving mirror', 
      'mosquito netting', '5 liter container of water', 
      'case of army rations', 'maps of the Atlantic Ocean', 
      'floating seat cushion', '10 liter can of oil/petrol mixture', 
      'small transistor radio', '20 square feet of opaque plastic sheeting', 
      'a can of shark repellent', 'a bottle of 160 proof rum', '15 feet of nylon rope', 
      '1 box of chocolate bars', 'an ocean fishing kit & pole']
  );
  const [robotName, setRobotName] = useState('');

  useEffect(() => {
    const eventOrderIndex = JSON.parse(sessionStorage.getItem('eventOrderIndex')) || 0;

    if (eventOrderIndex === 1 || eventOrderIndex === 2) {
        setRobotName('Star');
    } else {
        setRobotName('Luna');
    }
  }, []);

  const goToItemsPage = () => {
    navigate('/sea-initial-items');
  };

  const backgroundStyle = {
    backgroundImage: "url('media/sea-background.png')",
    backgroundSize: 'cover',
    height: '100vh',
    width: '100vw',
};

  return (
    <div className="page" style={backgroundStyle}>
      <h1 className="base-title">Sea Survival Simulation</h1>
      <p className='instructions'>
      You have chartered a yacht for the holiday trip of a lifetime across the Atlantic Ocean. 
      Unfortunately in mid Atlantic a fierce fire breaks out in the ships galley and the skipper 
      and crew have been lost whilst trying to fight the blaze. Much of the yacht is destroyed and 
      is slowly sinking. Your location is unclear because vital navigational and radio equipment 
      have been damaged in the fire. Your best estimate is that you are many hundreds of miles from the nearest 
      landfall. You have managed to save 15 items, undamaged and intact after the fire. In addition, 
      you have salvaged a four man rubber life craft and a box of matches. You will discuss with {robotName} to decide on the 7 most important items out of 15.
      </p>
      <ul className="item-list">
        {objects.map((item, index) => (
          <li key={index} className="item">{item}</li>
        ))}
      </ul>
      <button className="base-next-button" onClick={goToItemsPage}>Next</button>
    </div>
  );
}

export default SeaExplanationPage;