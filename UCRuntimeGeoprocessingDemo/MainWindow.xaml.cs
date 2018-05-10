using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Geoprocessing;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace ArcGISRuntime.WPF.Samples.AnalyzeHotspots
{
    public partial class AnalyzeHotspots
    {
        // Url for the geoprocessing service
        private const string _hotspotUrl =
            "http://sampleserver6.arcgisonline.com/arcgis/rest/services/911CallsHotspot/GPServer/911%20Calls%20Hotspot";

        // The geoprocessing task for hot spot analysis 
        private GeoprocessingTask _hotspotTask;

        // The job that handles the communication between the application and the geoprocessing task
        private GeoprocessingJob _hotspotJob;

        public AnalyzeHotspots()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            // Create a map with a topographic basemap
            Map myMap = new Map(Basemap.CreateTopographic());

            // Create a new geoprocessing task
            _hotspotTask = new GeoprocessingTask(new Uri(_hotspotUrl));

            // Assign the map to the MapView
            MyMapView.Map = myMap;
        }

        private async void OnAnalyzeHotspotsClicked(object sender, RoutedEventArgs e)
        {
            // Show the busyOverlay indication
            ShowBusyOverlay();

            // Get the 'from' and 'to' dates from the date pickers for the geoprocessing analysis
            var myFromDate = FromDate.SelectedDate.Value;
            var myToDate = ToDate.SelectedDate.Value;


            // The end date must be at least one day after the start date
            if (myToDate <= myFromDate.AddDays(1))
            {
                // Show error message
                MessageBox.Show(
                    "Please select valid time range. There has to be at least one day in between To and From dates.",
                    "Invalid date range");

                // Remove the busyOverlay
                ShowBusyOverlay(false);
                return;
            }

            // Create the parameters that are passed to the used geoprocessing task
            GeoprocessingParameters myHotspotParameters = new GeoprocessingParameters(GeoprocessingExecutionType.AsynchronousSubmit);

            // Construct the date query
            var myQueryString = string.Format("(\"DATE\" > date '{0} 00:00:00' AND \"DATE\" < date '{1} 00:00:00')",
                myFromDate.ToString("yyyy-MM-dd"),
                myToDate.ToString("yyyy-MM-dd"));

            // Add the query that contains the date range used in the analysis
            myHotspotParameters.Inputs.Add("Query", new GeoprocessingString(myQueryString));

            // Create job that handles the communication between the application and the geoprocessing task
            _hotspotJob = _hotspotTask.CreateJob(myHotspotParameters);
            _hotspotJob.JobChanged += _hotspotJob_JobChanged;
            //Start the Geoprocessing job
            _hotspotJob.Start();

            try
            {
                // Execute the geoprocessing analysis and wait for the results
                GeoprocessingResult myAnalysisResult = await _hotspotJob.GetResultAsync();

                // Add results to a map using map server from a geoprocessing task
                // Load to get access to full extent
                await myAnalysisResult.MapImageLayer.LoadAsync();

                // Add the analysis layer to the map view
                MyMapView.Map.OperationalLayers.Add(myAnalysisResult.MapImageLayer);

                // Zoom to the results
                await MyMapView.SetViewpointAsync(new Viewpoint(myAnalysisResult.MapImageLayer.FullExtent));
            }
            catch (TaskCanceledException)
            {
                // This is thrown if the task is canceled. Ignore.
            }
            catch (Exception ex)
            {
                // Display error messages if the geoprocessing task fails
                if (_hotspotJob.Status == JobStatus.Failed && _hotspotJob.Error != null)
                    MessageBox.Show("Executing geoprocessing failed. " + _hotspotJob.Error.Message, "Geoprocessing error");
                else
                    MessageBox.Show("An error occurred. " + ex.ToString(), "Sample error");
            }
            finally
            {
                // Remove the busyOverlay
                ShowBusyOverlay(false);
            }
        }

        private void _hotspotJob_JobChanged(object sender, EventArgs e)
        {

            var job = sender as GeoprocessingJob;

            if (job.Error != null)
            {
                Console.WriteLine("Error starting Geoprocessing Task: " + job.Error.Message);
                return;
            }

            // Check the job status
            if (job.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Succeeded)
            {
                // Report job success
                Console.WriteLine("GP Task is complete!");
            }
            else if (job.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Failed)
            {
                // Report job failure
                Console.WriteLine("Unable to run geoprocessing task.");
            }
            else
            {
                // Job is still running, report last message
                Console.WriteLine(job.Messages[job.Messages.Count - 1].Message);
            }
            //throw new NotImplementedException();
        }

        private void OnCancelTaskClicked(object sender, RoutedEventArgs e)
        {
            // Cancel current geoprocessing job
            if (_hotspotJob.Status == JobStatus.Started)
                _hotspotJob.Cancel();

            // Hide the busyOverlay indication
            ShowBusyOverlay(false);
        }

        private void ShowBusyOverlay(bool visibility = true)
        {
            // Function to toggle the visibility of interaction with the GUI for the user to 
            // specify dates for the hot spot analysis. When the analysis is running, the GUI
            // for changing the dates is 'grayed-out' and the progress bar with a cancel 
            // button (aka. busyOverlay object) becomes active.

            if (visibility)
            {
                // The geoprocessing task is processing. The busyOverly is present.
                busyOverlay.Visibility = Visibility.Visible;
                progress.IsIndeterminate = true;
            }
            else
            {
                // The user can interact with the date pickers. The busyOverlay is invisible.
                busyOverlay.Visibility = Visibility.Collapsed;
                progress.IsIndeterminate = false;
            }
        }
    }
}