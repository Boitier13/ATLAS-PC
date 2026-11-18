import React, { useState } from 'react';
import { TopTab, NavigationSection, SystemInfo, InstalledSoftware, InventorySnapshot } from './types';
import { SYSTEM_INFO_DEFAULT, INSTALLED_SOFTWARE_DEFAULT, INVENTORY_HISTORY_DEFAULT } from './data/componentsData';
import { WindowHeader } from './components/WindowHeader';
import { LeftSidebar } from './components/LeftSidebar';
import { StatusBar } from './components/StatusBar';
import { OverviewView } from './components/views/OverviewView';
import { SoftwareView } from './components/views/SoftwareView';
import { HardwareView } from './components/views/HardwareView';
import { InventoryHistoryView } from './components/views/InventoryHistoryView';
import { CompareView } from './components/views/CompareView';
import { ReportsView } from './components/views/ReportsView';
import { SettingsView } from './components/views/SettingsView';
import { AboutView } from './components/views/AboutView';

export default function App() {
  const [activeTab, setActiveTab] = useState<TopTab>('accueil');
  const [currentSection, setCurrentSection] = useState<NavigationSection>('overview');
  const [isDarkMode, setIsDarkMode] = useState<boolean>(false);

  const [systemInfo] = useState<SystemInfo>(SYSTEM_INFO_DEFAULT);
  const [softwareList] = useState<InstalledSoftware[]>(INSTALLED_SOFTWARE_DEFAULT);
  const [inventoryHistory, setInventoryHistory] = useState<InventorySnapshot[]>(INVENTORY_HISTORY_DEFAULT);

  // Synchronize top tabs with left section when clicking top ribbon
  const handleTabChange = (tab: TopTab) => {
    setActiveTab(tab);
    if (tab === 'accueil') setCurrentSection('overview');
    else if (tab === 'inventaire') setCurrentSection('history');
    else if (tab === 'comparer') setCurrentSection('history'); // or dedicated compare
    else if (tab === 'rapports') setCurrentSection('overview');
    else if (tab === 'parametres') setCurrentSection('settings');
    else if (tab === 'apropos') setCurrentSection('about');
  };

  const handleNewInventory = () => {
    alert("La collecte réelle des informations du PC n'est pas encore disponible dans cette version de démonstration. Aucun inventaire fictif n'a été créé.");
  };

  const handleDeleteInventory = (id: string) => {
    if (confirm('Voulez-vous vraiment supprimer cet inventaire de l\'historique ?')) {
      setInventoryHistory(prev => prev.filter(inv => inv.id !== id));
    }
  };

  return (
    <div className={`min-h-screen flex flex-col font-sans select-none transition-colors duration-200 ${
      isDarkMode ? 'bg-slate-950 text-slate-100' : 'bg-slate-200 text-slate-800'
    }`}>
      {/* Window Frame Header */}
      <WindowHeader
        activeTab={activeTab}
        setActiveTab={handleTabChange}
        isDarkMode={isDarkMode}
        setIsDarkMode={setIsDarkMode}
        onNewInventory={handleNewInventory}
      />

      {/* Main Window Body: Sidebar + Content Area */}
      <div className="flex-1 flex overflow-hidden">
        <LeftSidebar
          currentSection={currentSection}
          setCurrentSection={(sec) => {
            setCurrentSection(sec);
            if (sec === 'overview') setActiveTab('accueil');
            else if (sec === 'software') setActiveTab('inventaire');
            else if (sec === 'hardware') setActiveTab('inventaire');
            else if (sec === 'history') setActiveTab('inventaire');
            else if (sec === 'settings') setActiveTab('parametres');
            else if (sec === 'about') setActiveTab('apropos');
          }}
          isDarkMode={isDarkMode}
        />

        <main className={`flex-1 overflow-y-auto p-6 ${isDarkMode ? 'bg-slate-950/80' : 'bg-slate-50'}`}>
          {/* Render Active View based on currentSection or activeTab */}
          {activeTab === 'comparer' || currentSection === 'restore' || currentSection === 'delete' ? (
            <CompareView inventoryHistory={inventoryHistory} isDarkMode={isDarkMode} />
          ) : activeTab === 'rapports' ? (
            <ReportsView isDarkMode={isDarkMode} />
          ) : activeTab === 'parametres' || currentSection === 'settings' ? (
            <SettingsView isDarkMode={isDarkMode} setIsDarkMode={setIsDarkMode} />
          ) : activeTab === 'apropos' || currentSection === 'about' ? (
            <AboutView isDarkMode={isDarkMode} />
          ) : currentSection === 'software' ? (
            <SoftwareView softwareList={softwareList} isDarkMode={isDarkMode} />
          ) : currentSection === 'hardware' ? (
            <HardwareView systemInfo={systemInfo} isDarkMode={isDarkMode} />
          ) : currentSection === 'history' || currentSection === 'backup' ? (
            <InventoryHistoryView
              inventoryHistory={inventoryHistory}
              isDarkMode={isDarkMode}
              onNewInventory={handleNewInventory}
              onDeleteInventory={handleDeleteInventory}
            />
          ) : (
            <OverviewView
              systemInfo={systemInfo}
              inventoryHistory={inventoryHistory}
              isDarkMode={isDarkMode}
              onNewInventory={handleNewInventory}
              onViewHistory={() => setCurrentSection('history')}
              onCompare={() => setActiveTab('comparer')}
            />
          )}
        </main>
      </div>

      {/* Windows Status Bar */}
      <StatusBar isDarkMode={isDarkMode} lastInventoryDate={inventoryHistory[0]?.date || 'Données de démonstration'} />
    </div>
  );
}
