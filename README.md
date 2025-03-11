# How to run:
* Install grafana and Prometheus (externally)
* Setup prometheus as present in the configuration files (small adjustments may or may not be needed depending on the system)
* Add prometheus to grafana as a data source
* Import the dashboard from the provided dashboard.JSON

# Run stress tests
* Install the selenium IDE browser extension (I chose selenium as direct browser selection is the best way to get around antiforgery)
* Run the project and login on the Webapp
* Open the selenium extension
* Select open an existing project
* Select the provided as_test_load.side file
* (adjust the number of items to add (in the first instruction) to whatever value is needed)
* click "run test"

# Sequence diagram

![Sequence Diagram](sequence_diagram.png)

Note: this project was done on Ubuntu