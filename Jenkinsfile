/**
 * ApiService JenkinsFile for the CI and CD pipelines of Microservices and Monolith services.
 * ~~ MANAGED BY DEVOPS ~~
 */

/**
 * By default the master branch of the library is loaded
 * Use the include directive below ONLY if you need to load a branch of the library
 * @Library('intellifloworkflow@IP-17288')
 */
import org.intelliflo.*

def changeset = new Changeset()
def amazon = new Amazon()

def artifactoryCredentialsId = 'a3c63f46-4be7-48cc-869b-4239a869cbe8'
def artifactoryUri = 'https://artifactory.intelliflo.io/artifactory'
def ec2TemplateUrl = 'https://s3-eu-west-1.amazonaws.com/devops-aws/templates/ec2.subsys.vpc.microservice.template'
def codedeployTemplateUrl = 'https://s3-eu-west-1.amazonaws.com/devops-aws/templates/codedeploy.generic.template'
def gitCredentialsId = '1327a29c-d426-4f3d-b54a-339b5629c041'
def gitCredentialsSSH = 'jenkinsgithub'
def jiraCredentialsId = '32546070-393c-4c45-afcd-8e8f1de1757b'
def globals = env

def stageName
def semanticVersion
def packageVersion
def packageMd5
def stackName
def verboseLogging = false

// ############################################################################
// DEBUG PURPOSES ONLY
// Use these bypass switches ONLY if testing changes further on in the pipeline
def bypassSubsystemStage = false
def bypassSystemStage = false
// ############################################################################

pipeline {

    agent none

    environment {
        githubRepoName = "${env.JOB_NAME.split('/')[1]}"
        solutionName = "${env.JOB_NAME.split('/')[1].replace('Clone.', '')}"
    }

    options {
        timestamps()
    }

    stages {
        stage('Initialise') {
            agent none
            steps {
                script {
                    stashResourceFiles {
                        targetPath = 'org/intelliflo'
                        masterNode = 'master'
                        stashName = 'ResourceFiles'
                        resourcePath = "@libs/intellifloworkflow/resources"
                    }

                    abortOlderBuilds {
                        logVerbose = verboseLogging
                    }
                }
            }
        }

        stage('Component') {

            agent {
                label 'windows'
            }

            steps {
                bat 'set'

                script {
                    stageName = 'Component'

                    // Analyse and validate the changeset
                    validateChangeset {
                        repoName = globals.githubRepoName
                        prNumber = globals.CHANGE_ID
                        baseBranch = globals.CHANGE_TARGET
                        branchName = globals.BRANCH_NAME
                        buildNumber = globals.BUILD_NUMBER
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                        abortOnFailure = true
                    }
                    def json = (String)Consul.getStoreValue(ConsulKey.get(globals.githubRepoName, globals.BRANCH_NAME, globals.CHANGE_ID, 'changeset'))
                    changeset = changeset.fromJson(json)

                    // Checkout the code and unstash supporting scripts
                    checkoutCode {
                        delegate.stageName = stageName
                    }

                    // Scripts required by the pipeline
                    unstashResourceFiles {
                        folder = 'pipeline'
                        stashName = 'ResourceFiles'
                    }

                    // Versioning
                    calculateVersion {
                        buildNumber = globals.BUILD_NUMBER
                        delegate.changeset = changeset
                        delegate.stageName = stageName
                        abortOnFailure = true
                    }

                    semanticVersion = Consul.getStoreValue(ConsulKey.get(globals.githubRepoName, globals.BRANCH_NAME, globals.CHANGE_ID, 'existing.version'))
                    packageVersion = "${semanticVersion}.${env.BUILD_NUMBER}"
                    if (changeset.pullRequest != null) {
                        currentBuild.displayName = "${githubRepoName}.Pr${changeset.prNumber}(${packageVersion})"
                    } else {
                        currentBuild.displayName = "${githubRepoName}(${packageVersion})"
                    }
                    stackName = amazon.getStackName(env.githubRepoName, packageVersion, false, false)

                    startSonarQubeAnalysis {
                        repoName = globals.githubRepoName
                        solutionName = globals.solutionName
                        version = semanticVersion
                        unitTestResults = "UnitTestResults"
                        coverageResults = "OpenCoverResults"
                        inspectCodeResults = "ResharperInspectCodeResults"
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    createVersionTargetsFile {
                        serviceName = globals.solutionName
                        version = packageVersion
                        sha = changeset.commitSha
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    buildSolution {
                        solutionFile = "${globals.solutionName}.sln"
                        configuration = 'Release'
                        targetFramework = 'v4.5.2'
                        includeSubsystemTests = true
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    stashSubSystemTests {
                        solutionName = globals.solutionName
                        stashName = 'SubSystemTests'
                        delegate.stageName = stageName
                    }

                    def unitTestResults = runUnitTests {
                        title = "Unit Tests"
                        withCoverage = true
                        include = "test\\${globals.solutionName}.Tests\\bin\\Release\\${globals.solutionName}.Tests.dll"
                        unitTestsResultsFilename = "UnitTestResults"
                        coverageInclude = globals.solutionName
                        coverageResultsFilename = "OpenCoverResults"
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    runResharperInspectCode {
                        repoName = globals.githubRepoName
                        solutionName = globals.solutionName
                        resultsFile = "ResharperInspectCodeResults"
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    completeSonarQubeAnalysis {
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    analyseTestResults {
                        title = "Unit Tests"
                        testResults = unitTestResults
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    createNugetPackages {
                        createSubsysJsonFile = true
                        serviceName = changeset.serviceName
                        updateModConfigJsonFile = true
                        stack = stackName
                        version = packageVersion
                        artifactFolder = 'dist'
                        stashPackages = true
                        stashName = 'Packages'
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    findAndDeleteOldPackages {
                        credentialsId = artifactoryCredentialsId
                        packageName = "${changeset.repoName}.${semanticVersion}"
                        latestBuildNumber = globals.BUILD_NUMBER
                        url = artifactoryUri
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }
                }
            }

            post {
                success {
                    script {
                        if (changeset.pullRequest != null) {
                            publishPackages {
                                credentialsId = artifactoryCredentialsId
                                repo = 'nuget-snapshot'
                                version = packageVersion
                                include = "*.nupkg"
                                uri = artifactoryUri
                                properties = "github.pr.number=${changeset.prNumber} git.repo.name=${changeset.repoName} git.master.mergebase=${changeset.masterSha} jira.ticket=${changeset.jiraTicket}"
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }

                            packageMd5 = getMd5Sum {
                                repoName = globals.githubRepoName
                                version = packageVersion
                            }
                        }
                    }
                }
                always {
                    script {
                        if (changeset.pullRequest != null) {
                            publishToSplunk {
                                stage = stageName
                                repoName = changeset.repoName
                                prNumber = changeset.prNumber
                                version = packageVersion
                                outputToFile = true
                                outputToLog = false
                                consulKey = changeset.consulBuildKey
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }
                        }
                        archive excludes: 'dist/*.zip,dist/*.nupkg,dist/*.md5', includes: 'dist/*.*'
                    }
                }
            }
        }

        stage('SubSystem') {

            agent {
                label 'windows'
            }

            steps {
                script {
                    stageName = 'SubSystem'
                    if (!bypassSubsystemStage) {

                        prepareSubSystemStage {
                            solutionName = globals.solutionName
                            subsystemTestsStashName = 'SubSystemTests'
                            resourceFilesFolder = 'pipeline'
                            resourceFilesStashName = 'ResourceFiles'
                            artifactFolder = 'dist'
                            packagesStashName = 'Packages'
                            delegate.stageName = stageName
                        }

                        prepareCodeDeployPackages {
                            isMicroservice = changeset.isMicroservice
                            bucket = "codeartefacts"
                            filter = "${changeset.repoName}.*.nupkg"
                            artifactFolder = 'dist'
                            credentials = artifactoryCredentialsId
                            delegate.artifactoryUri = artifactoryUri
                            consulBuildKey = changeset.consulBuildKey
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        createEc2Stack {
                            stack = stackName
                            templateUrl = ec2TemplateUrl
                            consulKey = changeset.consulBuildKey
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        labelStackResources {
                            repoName = changeset.repoName
                            version = packageVersion
                            consulKey = changeset.consulBuildKey
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        deployToAws {
                            packageName = changeset.repoName
                            version = packageVersion
                            isMicroservice = changeset.isMicroservice
                            instance = "microservice"
                            serviceAction = "start"
                            templateUrl = codedeployTemplateUrl
                            consulKey = changeset.consulBuildKey
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        checkServiceHealth {
                            serverDns = Consul.getStoreValue("${changeset.consulBuildKey}/MicroserviceAddress")
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        if (changeset.pullRequest != null) {
                            verifyPackageExists {
                                packageName = changeset.repoName
                                version = packageVersion
                                uri = artifactoryUri
                                repo = "nuget-snapshot"
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }
                        }

                        prepareSubSystemTestConfigFile {
                            solutionName = globals.solutionName
                            configuration = 'Debug'
                            stack = stackName
                            consulKey = changeset.consulBuildKey
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        def subsystemTestResults = runUnitTests {
                            title = "SubSystem Tests"
                            withCoverage = false
                            include = "test\\${globals.solutionName}.SubSystemTests\\bin\\Debug\\${globals.solutionName}.SubSystemTests.dll"
                            unitTestsResultsFilename = "SubSystemTestResults"
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        analyseTestResults {
                            title = "SubSystem Tests"
                            testResults = subsystemTestResults
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        measureSubSystemCoverage {
                            repoName = changeset.repoName
                            solutionName = globals.solutionName
                            serviceName = changeset.serviceName
                            consulKey = changeset.consulBuildKey
                            artifactFolder = 'dist'
                            warnIfProdUrlNotAvailable = true
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }
                    } else {
                        echo "[DEBUG] Bypassing ${stageName} Stage"
                    }
                }
            }

            post {
                success {
                    script {
                        findAndDeleteOldPackages {
                            credentialsId = artifactoryCredentialsId
                            packageName = "${changeset.repoName}.${semanticVersion}"
                            latestBuildNumber = globals.BUILD_NUMBER
                            url = artifactoryUri
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        if (changeset.pullRequest != null) {
                            promotePackage {
                                packageName = changeset.repoName
                                version = packageVersion
                                packageMasterSha = changeset.masterSha
                                sourceRepo = 'nuget-snapshot'
                                destinationRepo = 'nuget-ready4test'
                                credentialsId = artifactoryCredentialsId
                                url = artifactoryUri
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }
                        }

                        if (changeset.branch != null) {
                            publishPackages {
                                credentialsId = artifactoryCredentialsId
                                repo = 'nuget-dev-snapshot'
                                version = packageVersion
                                include = "*.nupkg"
                                uri = artifactoryUri
                                properties = "git.branch.name=${changeset.branchName} git.repo.name=${changeset.repoName} git.master.mergebase=${changeset.masterSha} jira.ticket=${changeset.jiraTicket}"
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }

                            addDeployLink {
                                packageName = changeset.repoName
                                delegate.packageVersion = packageVersion
                            }
                        }
                    }
                }
                always {
                    script {
                        if (!bypassSubsystemStage) {
                            addSubSystemLogsToArtifacts {
                                stack = stackName
                                consulKey = changeset.consulBuildKey
                                artifactFolder = 'dist'
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }

                            removeAwsStacks {
                                repoName = changeset.repoName
                                version = packageVersion
                                isMicroservice = changeset.isMicroservice
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }

                            deleteS3Package {
                                bucketName = 'codeartefacts'
                                name = changeset.repoName
                                version = packageVersion
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }

                            publishToSplunk {
                                stage = stageName
                                repoName = changeset.repoName
                                prNumber = changeset.prNumber
                                version = packageVersion
                                outputToFile = true
                                outputToLog = false
                                consulKey = changeset.consulBuildKey
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }
                        }
                        archive excludes: 'dist/*.zip,dist/*.nupkg,dist/*.md5', includes: 'dist/*.*'
                    }
                }
            }
        }

        stage('System') {

            agent none

            when {
                expression {
                    return env.BRANCH_NAME.startsWith('PR')
                }
            }
            steps {
                script {
                    stageName = 'System'
                    if (!bypassSystemStage) {
                        def deploy = pauseForInput {
                            delegate.stageName = stageName
                            message = 'SIT testing required?'
                            okButtonText = 'Yes'
                            logVerbose = verboseLogging
                        }

                        if (deploy) {
                            deployToEnvironment {
                                delegate.stageName = stageName
                                repoName = changeset.repoName
                                solutionName = globals.solutionName
                                serviceName = changeset.serviceName
                                prNumber = changeset.prNumber
                                delegate.packageVersion = packageVersion
                                targetRepo = "nuget-ready4test"
                                delegate.artifactoryUri = artifactoryUri
                                packageMasterSha = changeset.masterSha
                                playbook = "api-service"
                                deploySlaveLabel = 'deploy'
                                deployScriptsBranchName = 'master'
                                gitCredentials = gitCredentialsSSH
                                logVerbose = verboseLogging
                                packageMd5Checksum = packageMd5
                            }
                        }

                        def successful = pauseForInput {
                            delegate.stageName = stageName
                            message = 'Manual SIT testing successful?'
                            okButtonText = 'Yes'
                            logVerbose = verboseLogging
                        }

                        if (!successful) {
                            currentBuild.result = 'ABORTED'
                            error "SIT Testing unsuccessful"
                        }
                    } else {
                        echo "[DEBUG] Bypassing ${stageName} Stage"
                    }
                }
            }

            post {
                success {
                    script {
                        node('windows') {
                            unstashResourceFiles {
                                folder = 'pipeline'
                                stashName = 'ResourceFiles'
                            }

                            findAndDeleteOldPackages {
                                credentialsId = artifactoryCredentialsId
                                packageName = "${changeset.repoName}.${semanticVersion}"
                                latestBuildNumber = globals.BUILD_NUMBER
                                url = artifactoryUri
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }

                            promotePackage {
                                packageName = changeset.repoName
                                version = packageVersion
                                packageMasterSha = changeset.masterSha
                                sourceRepo = 'nuget-ready4test'
                                destinationRepo = 'nuget-ready4prd'
                                credentialsId = artifactoryCredentialsId
                                url = artifactoryUri
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }
                        }
                    }
                }
                always {
                    script {
                        if (!bypassSystemStage) {
                            node('windows') {
                                bat "if not exist dist\\NUL (mkdir dist)"

                                unstashResourceFiles {
                                    folder = 'pipeline'
                                    stashName = 'ResourceFiles'
                                }

                                publishToSplunk {
                                    stage = stageName
                                    repoName = changeset.repoName
                                    prNumber = changeset.prNumber
                                    version = packageVersion
                                    outputToFile = true
                                    outputToLog = false
                                    consulKey = changeset.consulBuildKey
                                    logVerbose = verboseLogging
                                    delegate.stageName = stageName
                                }

                                archive excludes: 'dist/*.zip,dist/*.nupkg,dist/*.md5', includes: 'dist/*.*'
                            }
                        }
                    }
                }
            }
        }

        stage('Production') {

            agent none

            when {
                expression {
                    return env.BRANCH_NAME.startsWith('PR')
                }
            }
            steps {
                script {
                    stageName = 'Production'

                    validateJiraTicket {
                        delegate.changeset = changeset
                        failBuild = false
                        delegate.stageName = stageName
                        logVerbose = verboseLogging
                    }

                    deployToProduction {
                        delegate.stageName = stageName
                        repoName = changeset.repoName
                        solutionName = globals.solutionName
                        serviceName = changeset.serviceName
                        prNumber = changeset.prNumber
                        delegate.packageVersion = packageVersion
                        targetRepo = "nuget-ready4prd"
                        delegate.artifactoryUri = artifactoryUri
                        packageMasterSha = changeset.masterSha
                        playbook = "api-service"
                        deploySlaveLabel = 'deploy'
                        deployScriptsBranchName = 'master'
                        gitCredentials = gitCredentialsSSH
                        logVerbose = verboseLogging
                        packageMd5Checksum = packageMd5
                    }

                    def successful = pauseForInput {
                        withTimeout = {
                            time = 3
                            unit = 'HOURS'
                        }
                        delegate.stageName = stageName
                        message = 'Has manual PRD testing been successful?'
                        okButtonText = 'Yes'
                        logVerbose = verboseLogging
                    }

                    if (!successful) {
                        currentBuild.result = 'ABORTED'
                        error "PRD Testing unsuccessful"
                    }

                    verifyPackageExists {
                        packageName = changeset.repoName
                        version = packageVersion
                        uri = artifactoryUri
                        repo = 'nuget-ready4prd'
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    validateMasterSha {
                        repoName = changeset.repoName
                        packageMasterSha = changeset.masterSha
                        logVerbose = verboseLogging
                        delegate.stageName = stageName
                    }

                    node('windows') {
                        mergePullRequest {
                            repoName = changeset.repoName
                            prNumber = changeset.prNumber
                            masterSha = changeset.masterSha
                            sha = changeset.commitSha
                            consulKey = changeset.consulBaseKey
                            credentialsId = gitCredentialsId
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }
                    }
                }
            }

            post {
                success {
                    script {
                        node('windows') {
                            unstashResourceFiles {
                                folder = 'pipeline'
                                stashName = 'ResourceFiles'
                            }

                            promotePackage {
                                packageName = changeset.repoName
                                version = packageVersion
                                packageMasterSha = changeset.masterSha
                                sourceRepo = 'nuget-ready4prd'
                                destinationRepo = 'nuget-prd'
                                force = true
                                credentialsId = artifactoryCredentialsId
                                url = artifactoryUri
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }

                            updateJiraOnMerge {
                                issueKey = changeset.jiraTicket
                                packageName = changeset.repoName
                                version = packageVersion
                                credentialsId = jiraCredentialsId
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }
                        }

                        updateWatermarks {
                            repoName = changeset.repoName
                            consulBuildKey = changeset.consulBuildKey
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        tagCommit {
                            repoName = changeset.repoName
                            version = semanticVersion
                            author = changeset.author
                            email = changeset.commitInfo.author.email
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        updateMasterVersion {
                            repoName = changeset.repoName
                            version = semanticVersion
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        cleanupConsul {
                            repoName = changeset.repoName
                            prNumber = changeset.prNumber
                            consulBuildKey = changeset.consulBuildKey
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }
                    }
                }
                always {
                    script {
                        releaseProductionStage {
                            repoName = changeset.repoName
                            logVerbose = verboseLogging
                            delegate.stageName = stageName
                        }

                        node('windows') {
                            bat "if not exist dist\\NUL (mkdir dist)"

                            unstashResourceFiles {
                                folder = 'pipeline'
                                stashName = 'ResourceFiles'
                            }

                            publishToSplunk {
                                stage = stageName
                                repoName = changeset.repoName
                                prNumber = changeset.prNumber
                                version = packageVersion
                                outputToFile = true
                                outputToLog = false
                                consulKey = changeset.consulBuildKey
                                logVerbose = verboseLogging
                                delegate.stageName = stageName
                            }

                            archive excludes: 'dist/*.zip,dist/*.nupkg,dist/*.md5', includes: 'dist/*.*'
                        }
                    }
                }
            }
        }
    }

    post {
        success {
            node ('windows') {
                deleteDir()
            }
        }
    }
}
