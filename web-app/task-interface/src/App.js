import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import EventSelectionPage from './pages/EventSelectionPage';
import StarInstructionPage from './pages/StarInstructionPage';
import LunaInstructionPage from './pages/LunaInstructionPage';
import PracticeTaskPage from './pages/PracticeTaskPage';

import DesertIntroPage from './pages/survival/desert/DesertIntroPage';
import DesertExplanationPage from './pages/survival/desert/DesertExplanationPage';
import DesertInitialItemsPage from './pages/survival/desert/DesertInitialItemsPage';
import DesertTaskPage from './pages/survival/desert/DesertTaskPage';
import DesertEndPage from './pages/survival/desert/DesertEndPage';

import SeaIntroPage from './pages/survival/sea/SeaIntroPage';
import SeaExplanationPage from './pages/survival/sea/SeaExplanationPage';
import SeaInitialItemsPage from './pages/survival/sea/SeaInitialItemsPage';
import SeaTaskPage from './pages/survival/sea/SeaTaskPage';
import SeaEndPage from './pages/survival/sea/SeaEndPage';

import DiscussionExplanationPage from './pages/discussion/discussion-1/DiscussionExplanationPage';
import DiscussionTaskPage from './pages/discussion/discussion-1/DiscussionTaskPage';
import DiscussionEndPage from './pages/discussion/discussion-1/DiscussionEndPage';
import DiscussionExplanationPage2 from './pages/discussion/discussion-2/DiscussionExplanationPage2';
import DiscussionTaskPage2 from './pages/discussion/discussion-2/DiscussionTaskPage2';
import DiscussionEndPage2 from './pages/discussion/discussion-2/DiscussionEndPage2';

import './App.css';

function App() {
  return (
    <Router>
       <Routes>
       <Route path="/" element={<EventSelectionPage />} />
        <Route path="/star-instructions" element={<StarInstructionPage />} />
        <Route path="/luna-instructions" element={<LunaInstructionPage />} />
        <Route path="/practice-task" element={<PracticeTaskPage />} />


        {/* Desert Survival Task */}
        <Route path="/desert-intro" element={<DesertIntroPage />} />
        <Route path="/desert-explanation" element={<DesertExplanationPage />} />
        <Route path="/desert-initial-items" element={<DesertInitialItemsPage />} />
        <Route path="/desert-task" element={<DesertTaskPage />} />
        <Route path="/desert-end" element={<DesertEndPage />} />

        {/* Ocean Survival Task */}
        <Route path="/sea-intro" element={<SeaIntroPage />} />
        <Route path="/sea-explanation" element={<SeaExplanationPage />} />
        <Route path="/sea-initial-items" element={<SeaInitialItemsPage />} />
        <Route path="/sea-task" element={<SeaTaskPage />} />
        <Route path="/sea-end" element={<SeaEndPage />} />

        {/* AI Discussion Task */}
        <Route path="/discussion-explanation" element={<DiscussionExplanationPage />} />
        <Route path="/discussion-task" element={<DiscussionTaskPage />} />
        <Route path="/discussion-end" element={<DiscussionEndPage />} />
        
        {/* Capital Punishment Survival Task */}
        <Route path="/discussion-explanation-2" element={<DiscussionExplanationPage2 />} />
        <Route path="/discussion-task-2" element={<DiscussionTaskPage2 />} />
        <Route path="/discussion-end-2" element={<DiscussionEndPage2 />} />
      </Routes>
    </Router>
  );
}

export default App;