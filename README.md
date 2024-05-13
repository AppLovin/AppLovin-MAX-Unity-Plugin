# AppLovin MAX Unity Plugin Demo App
**⚠️ Note:** This repository contains the Unity demo app for MAX. The Releases tab only hosts release notes. To download and install the MAX Unity Plugin, follow the instructions at [AppLovin's Unity integration documentation](https://developers.applovin.com/en/unity/overview/integration).

## Overview
MAX is AppLovin's in-app monetization solution.

Move beyond the traditional monetization solution and integrate MAX. MAX is a single unbiased auction where advertisers get equal access to all ad inventory and bid simultaneously, which drives more competition and higher CPMs for you. You can read more about it [here](https://www.applovin.com/max-header-bidding).

Please check out our [documentation](https://developers.applovin.com/en/unity/overview/integration) to get started on integrating and enabling mediated networks using our guides.

## Demo Apps
To get started with the demo apps, follow the instructions below:

1. Clone this project and open the DemoApp project in the Unity Editor.
2. Under `File > Build Settings`, click on the mobile platform of choice and click on `Player Settings`.
3. Update the iOS bundle identifier and/or Android package name with your own unique identifier(s) associated with the application you will create in the MAX dashboard (or already created, if it is an existing app).
4. Open the `HomeScreen.cs` file under `DemoApp/Assets/Scripts/`.
5. Update the `MaxSdkKey` value with your AppLovin SDK key associated with your account.
6. Update the MAX ad unit IDs for the ad formats you would like to test. Each ad format will correspond to a unique MAX ad unit ID you created in the AppLovin dashboard for the bundle id used before.
7. Under `File > Build Settings`, click on the mobile platform of choice and click `Switch Platform` then `Build` to create a project to test the Demo App.

<kbd><img src="https://user-images.githubusercontent.com/20387467/116736029-3262d280-a9a4-11eb-8b43-9889f49b9a3e.PNG" width="350" height="700" /></kbd>

## Support
We recommend using GitHub to file issues. For feature requests, improvements, questions or any other integration issues using MAX Mediation by AppLovin, please contact us via our support page: https://monetization-support.applovin.com/hc/en-us.
