require './common.iced'

# ==============================================================================
# tasks required for this build 
Tasks "dotnet"  # dotnet functions

# ==============================================================================
# Settings
Import
  initialized: false
  solution: "#{basefolder}/autorest.common.sln"
  sourceFolder:  "#{basefolder}/src/"

# ==============================================================================
# Tasks

task 'init', "" ,(done)->
  Fail "YOU MUST HAVE NODEJS VERSION GREATER THAN 7.10.0" if semver.lt( process.versions.node , "7.10.0" )
  done()
  
# Run language-specific tests:
# (ie, things that call stuff like 'mvn test', 'npm test', 'tox', 'go run' etc)
task 'test', "more", ["regenerate"], (done) ->
  # insert commands here to do other kinds of testing
  # echo "Testing More"
  done();
  
task 'pack', 'Create the nuget package', ['build'], (done) ->
  # create the nuget package 
  execute "dotnet pack -c #{configuration} #{sourceFolder} /nologo /clp:NoSummary /p:version=#{version}", (code, stdout, stderr) ->
    done()

task 'publish', 'publishes the package to nuget.org',['release-only','version-number'] ,(done)->
  # must be --release to publish the package 
  run ['pack'], ->
    execute "#{basefolder}/tools/nuget.exe push #{basefolder}/src/bin/autorest.common.#{version}.nupkg #{nuget_apikey} -source nuget.org", (c,o,e) ->
      done()