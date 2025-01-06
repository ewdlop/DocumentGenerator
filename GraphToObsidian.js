const fs = require('fs');
const path = require('path');

class GraphToObsidian {
    constructor(outputDir) {
        this.outputDir = outputDir;
        this.ensureDirectoryExists(outputDir);
    }

    // Ensure the output directory exists
    ensureDirectoryExists(dirPath) {
        if (!fs.existsSync(dirPath)) {
            fs.mkdirSync(dirPath, { recursive: true });
        }
    }

    // Convert node title to valid filename
    sanitizeFilename(title) {
        return title.replace(/[^a-z0-9]/gi, ' ').trim().replace(/\s+/g, '-').toLowerCase();
    }

    // Create a markdown file for a node
    createNodeFile(node) {
        const filename = this.sanitizeFilename(node.title);
        const filePath = path.join(this.outputDir, `${filename}.md`);

        let content = `# ${node.title}\n\n`;
        
        // Add properties as YAML frontmatter
        if (node.properties && Object.keys(node.properties).length > 0) {
            content += '---\n';
            Object.entries(node.properties).forEach(([key, value]) => {
                content += `${key}: ${value}\n`;
            });
            content += '---\n\n';
        }

        // Add content
        if (node.content) {
            content += `${node.content}\n\n`;
        }

        // Add links section
        if (node.links && node.links.length > 0) {
            content += '## Links\n\n';
            node.links.forEach(link => {
                content += `- [[${link.target}]] - ${link.relationship}\n`;
            });
        }

        fs.writeFileSync(filePath, content);
        return filePath;
    }

    // Convert entire graph to Obsidian vault
    convertGraph(graph) {
        console.log('Starting conversion...');
        
        const createdFiles = [];
        
        // Process each node in the graph
        graph.nodes.forEach(node => {
            const filePath = this.createNodeFile(node);
            createdFiles.push(filePath);
            console.log(`Created file: ${filePath}`);
        });

        console.log('\nConversion complete!');
        console.log(`Created ${createdFiles.length} files in ${this.outputDir}`);
        
        return createdFiles;
    }
}

// Example usage:
const graph = {
    nodes: [
        {
            title: "Project Alpha",
            properties: {
                status: "active",
                priority: "high"
            },
            content: "This is the main project description.",
            links: [
                { target: "Task 1", relationship: "has task" },
                { target: "John Doe", relationship: "managed by" }
            ]
        },
        {
            title: "Task 1",
            properties: {
                deadline: "2024-02-01"
            },
            content: "Task details go here.",
            links: [
                { target: "Project Alpha", relationship: "part of" }
            ]
        }
    ]
};

// Create converter instance and run conversion
const converter = new GraphToObsidian('./obsidian-vault');
converter.convertGraph(graph);
