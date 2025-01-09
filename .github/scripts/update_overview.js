const fs = require("fs");
const path = require("path");

// Read package.json
const packageJson = JSON.parse(fs.readFileSync("package.json", "utf8"));

// Initialize markdown content
let markdown = `# ${packageJson.displayName}\n\n`;

// Check for overview image right after title
const imagePath = "Documentation~/images/overview/";
const packageName = packageJson.name;
const extensions = [".gif", ".png", ".jpg", ".jpeg"];
let imageFound = false;

for (const ext of extensions) {
    const imageName = packageName + ext;
    const fullPath = path.join(imagePath, imageName);

    if (fs.existsSync(fullPath)) {
        markdown += `![${packageName}](images/overview/${imageName})\n\n`;
        imageFound = true;
        break;
    }
}

// Description without heading
if (packageJson.description) {
    markdown += `${packageJson.description}\n\n`;
}

// Technical details section
markdown += "## Technical details\n\n";

// Package section
markdown += "### Package\n\n";
markdown += `* Name: ${packageJson.name}\n`;
markdown += `* Display name: ${packageJson.displayName}\n`;
markdown += `* Version: ${packageJson.version}\n`;

// Handle repository URL without any modifications
if (packageJson.repository) {
    const repoUrl = typeof packageJson.repository === "string" ?
        packageJson.repository :
        packageJson.repository.url;

    markdown += `* [Git URL](${repoUrl})\n`;
}

markdown += "\n";

// Requirements section
markdown += "### Requirements\n\n";
if (packageJson.unity) {
    markdown += `This version of the ${packageJson.displayName} package is compatible with the following versions of the Unity Editor:\n\n`;
    markdown += `* ${packageJson.unity} and later\n\n`;
}

// Dependencies section
if (packageJson.dependencies && Object.keys(packageJson.dependencies).length > 0) {
    markdown += `### Dependencies\n\n`;
    markdown += `The ${packageJson.displayName} package has the following dependencies which are automatically added to your project when installing:\n\n`;
    for (const [dep, version] of Object.entries(packageJson.dependencies)) {
        markdown += `* ${dep}@${version}\n`;
    }
    markdown += "\nIf the package has additional Git dependencies listed in the package description, such as XRC Toolkit packages, then they need to be installed manually via the Unity Package Manager.\n\n";
}

// Installation section
markdown += "### Installation\n\n";
markdown += "To install the package, follow these steps:\n";
markdown += "1. In the Unity Editor, click on **Window > Package Manager**\n";
markdown += "2. Click the + button and choose **Add package from git URL** option\n";
markdown += "3. Enter the package's Git URL. Make sure the URL has \".git\" ending.\n";
markdown += "4. If the repository is private, you will be asked to authenticate via your GitHub account. If you haven't been granted access to the repository you will not be able to install the package.\n";
markdown += "5. The package should be installed into your project.\n";
markdown += "6. You can download the package samples from under the Samples tab in the Package Manager\n\n";
markdown += "From Unity: [Install a UPM package from a Git URL](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-giturl.html)\n\n";
markdown += "> [!NOTE]\n";
markdown += "> Even though the package documentation is public, the package itself and its repository might be private and not accessible. If you haven't been granted access to the repository you will not be able to install the package.\n\n";

// Write to file
fs.writeFileSync("Documentation~/index.md", markdown);