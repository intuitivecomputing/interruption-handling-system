// ExplanationPage.js
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import '../css/SurvivalExplanationPage.css';

function DesertExplanationPage() {
  const navigate = useNavigate();
  const [objects] = useState(['flashlight', 'jackknife', 'air map of the area', 'magnetic compass', 'first-aid kit', 'bottle of salt tablets', '1 quart of water per person', '1 pair of sunglasses per person', '1 overcoat per person', 'a book entitled \'Edible Animals of the Desert\'', 'parachute', 'plastic raincoat', '45 caliber pistol', '2 bottles of vodka', 'cosmetic mirror']);
  const goToItemsPage = () => {
    navigate('/desert-initial-items');
  };

  const backgroundStyle = {
    backgroundImage: "url('media/desert-background.png')",
    backgroundSize: 'cover',
    height: '100vh',
    width: '100vw',
  };

  return (
    <div className="page" style={backgroundStyle}>
      <h1 className="base-title">Desert Survival Simulation</h1>
      <p className='instructions'>
          It is approximately 10am in mid-August and you have just crash landed in the Sonoran Desert,
          near the Mexico-USA border. Your  plane has completely burnt out, only the frame remains. 
          Ground sightings taken shortly before the crash suggest that you are about 65 miles southeast
          of a mining camp. The camp is the nearest known settlement. The immediate area is quite flat and,
          except for the occasional thorn bush and cacti, is rather barren. Before the plane caught fire,
          you were able to save 15 items below. You will discuss with Luna to decide on the 7 most important items out of 15.
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

export default DesertExplanationPage;