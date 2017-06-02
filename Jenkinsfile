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
        githubRepoName = "JustSaying"
        solutionName = "JustSaying"
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
    }
}
